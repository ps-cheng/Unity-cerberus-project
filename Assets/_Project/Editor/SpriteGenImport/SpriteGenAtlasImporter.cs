using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Cerberus.EditorTools.SpriteGenImport
{
    /// <summary>
    /// 把 sprite-gen 的角色圖集匯入 Unity：
    /// 1. 複製 manifest.json 和 sprite-sheet-alpha.png 到專案
    /// 2. 設定貼圖（手繪風格：Bilinear、高品質壓縮、不產生 mipmap）
    /// 3. 依 manifest 的 frame_layout 切圖，錨點放在腳底
    /// 4. 依每個狀態的 fps、每格時間、是否循環建立 Animation Clip
    /// 5. 確認共用 Animator Controller 有這些狀態，並把 clip 填進該角色的 Override Controller
    ///
    /// 重新匯入是安全的：
    /// - 同名 sprite 沿用原本的 ID，場景和資產裡對 sprite 的引用不會斷
    /// - clip 只覆寫 sprite 那條曲線，手動加的其他曲線（例如半透明、位移）會保留
    /// - Controller 已有的狀態只更新 clip 和播放速度設定，不動其他東西
    /// </summary>
    public static class SpriteGenAtlasImporter
    {
        private const string ManifestFileName = "manifest.json";
        private const string SheetFileName = "sprite-sheet-alpha.png";

        private static readonly EditorCurveBinding SpriteBinding =
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");

        public static void ImportAll(SpriteGenImportProfile profile)
        {
            var report = new StringBuilder($"[Sprite-Gen 匯入] PPU {profile.pixelsPerUnit}\n");
            foreach (SpriteGenImportProfile.CharacterEntry entry in profile.characters)
                ImportCharacter(profile, entry, report);

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        public static void ImportCharacter(SpriteGenImportProfile profile, SpriteGenImportProfile.CharacterEntry entry, StringBuilder report = null)
        {
            string id = entry.characterId;
            string runDir = Path.Combine(profile.runsRoot, id);
            SpriteGenManifest manifest = SpriteGenManifest.Load(Path.Combine(runDir, ManifestFileName));

            // 1. 複製到專案
            string atlasDir = $"{profile.atlasFolder}/{id}";
            EnsureFolder(atlasDir);
            CopyIntoProject(Path.Combine(runDir, ManifestFileName), $"{atlasDir}/{ManifestFileName}");
            string sheetPath = CopyIntoProject(Path.Combine(runDir, SheetFileName), $"{atlasDir}/{id}.png");

            // 2、3. 設定貼圖並切圖
            Dictionary<string, Sprite> sprites = ImportSheet(sheetPath, id, manifest, profile.pixelsPerUnit);

            // 換色版圖集（例如狂暴化）格子相同，只切圖
            foreach (SpriteGenImportProfile.VariantEntry variant in entry.variants)
            {
                string variantId = id + variant.suffix;
                string variantPath = CopyIntoProject(Path.Combine(runDir, variant.relativeSheetPath), $"{atlasDir}/{variantId}.png");
                ImportSheet(variantPath, variantId, manifest, profile.pixelsPerUnit);
            }

            // 4. 每個狀態一個 clip
            string clipDir = $"{profile.clipFolder}/{id}";
            EnsureFolder(clipDir);
            var clips = new Dictionary<string, AnimationClip>();
            foreach (SpriteGenManifest.State state in manifest.States)
                clips[state.Name] = BuildClip($"{clipDir}/{id}_{state.Name}.anim", state, id, sprites);

            // 5. 共用 Controller 和這個角色的 Override
            Dictionary<string, AnimationClip> baseClips = EnsureController(profile, manifest.States.Select(s => s.Name));
            AnimatorOverrideController overrides = EnsureOverride(profile, entry);
            ApplyOverrides(overrides, baseClips, clips);

            report?.AppendLine(DescribeImport(id, manifest, profile.pixelsPerUnit, entry.variants.Count));
        }

        // ---------- 貼圖與切圖 ----------

        private static Dictionary<string, Sprite> ImportSheet(string assetPath, string spritePrefix, SpriteGenManifest manifest, float pixelsPerUnit)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;          // 手繪風格，不是像素圖
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ; // 低品質壓縮會糊掉黑色描邊
            importer.maxTextureSize = 4096;                     // 圖集寬 2688～3072，2048 會被縮小

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // 沿用同名 sprite 的 ID，重新匯入時引用才不會斷
            Dictionary<string, GUID> existingIds = provider.GetSpriteRects()
                .GroupBy(r => r.name)
                .ToDictionary(g => g.Key, g => g.First().spriteID);

            var rects = new List<SpriteRect>();
            foreach (SpriteGenManifest.State state in manifest.States)
            {
                for (int i = 0; i < state.Cells.Length; i++)
                {
                    RectInt cell = state.Cells[i];
                    string name = SpriteName(spritePrefix, state.Name, i);
                    rects.Add(new SpriteRect
                    {
                        name = name,
                        // manifest 的原點在左上，Unity 的在左下
                        rect = new Rect(cell.x, manifest.SheetHeight - cell.y - cell.height, cell.width, cell.height),
                        alignment = SpriteAlignment.Custom,
                        pivot = manifest.FootPivot,
                        spriteID = existingIds.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                    });
                }
            }

            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture.width != manifest.SheetWidth || texture.height != manifest.SheetHeight)
                Debug.LogWarning($"{assetPath} 是 {texture.width}x{texture.height}，manifest 寫 {manifest.SheetWidth}x{manifest.SheetHeight}", texture);

            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToDictionary(s => s.name);
        }

        public static string SpriteName(string prefix, string state, int index) => $"{prefix}_{state}_{index}";

        // ---------- Animation Clip ----------

        private static AnimationClip BuildClip(string path, SpriteGenManifest.State state, string spritePrefix, Dictionary<string, Sprite> sprites)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = state.Fps;

            // 每格的開始時間依 durations_ms 累加。不在最後補一個重複的 key：
            // sprite 曲線的最後一格本身就會多佔 1/frameRate，補了會讓 clip 多一格長
            // durations_ms 是四捨五入的整數（6fps 寫成 167ms），每格都等於 1/fps 時直接用 i/fps，避免誤差累積
            float frameMs = 1000f / state.Fps;
            bool uniform = state.DurationsMs.All(d => Mathf.Abs(d - frameMs) <= 1f);

            var keys = new ObjectReferenceKeyframe[state.Cells.Length];
            float time = 0f;
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = uniform ? i / (float)state.Fps : time, value = sprites[SpriteName(spritePrefix, state.Name, i)] };
                time += state.DurationsMs[i] / 1000f;
            }

            // 只換掉 sprite 曲線，其他手動加的曲線保留
            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = state.Loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            int lastMs = state.DurationsMs[state.DurationsMs.Length - 1];
            if (Mathf.Abs(lastMs - frameMs) > 1f)
                Debug.LogWarning($"{path}: 最後一格 {lastMs}ms 跟 1/fps 不同，Unity 會用 1/fps 當最後一格的長度", clip);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        // ---------- Animator Controller ----------

        /// <summary>確認共用 Controller 有每個狀態，回傳「狀態名稱 → 該狀態的 Base clip」</summary>
        private static Dictionary<string, AnimationClip> EnsureController(SpriteGenImportProfile profile, IEnumerable<string> manifestStates)
        {
            if (profile.sharedController == null)
            {
                profile.sharedController = AnimatorController.CreateAnimatorControllerAtPath($"{profile.clipFolder}/Player.controller");
                EditorUtility.SetDirty(profile);
            }

            AnimatorController controller = profile.sharedController;
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            var baseClips = new Dictionary<string, AnimationClip>();

            foreach (string stateName in manifestStates)
            {
                SpriteGenImportProfile.StateSetup setup = profile.states.FirstOrDefault(s => s.stateName == stateName);
                if (setup == null)
                {
                    Debug.LogWarning($"Import Profile 沒有設定狀態 {stateName}，略過（clip 仍會建立）", profile);
                    continue;
                }

                AnimatorState state = stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName)
                    ?? stateMachine.AddState(stateName);

                // Base clip 只是給 Override 對應用的空 clip
                if (!(state.motion is AnimationClip baseClip))
                {
                    string basePath = $"{profile.clipFolder}/Base_{stateName}.anim";
                    baseClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(basePath);
                    if (baseClip == null)
                    {
                        baseClip = new AnimationClip();
                        AssetDatabase.CreateAsset(baseClip, basePath);
                    }
                    state.motion = baseClip;
                }

                ApplySpeed(controller, state, setup);
                baseClips[stateName] = baseClip;
            }

            EditorUtility.SetDirty(controller);
            return baseClips;
        }

        private static void ApplySpeed(AnimatorController controller, AnimatorState state, SpriteGenImportProfile.StateSetup setup)
        {
            bool useParameter = !string.IsNullOrEmpty(setup.speedParameter);
            if (useParameter && controller.parameters.All(p => p.name != setup.speedParameter))
            {
                controller.AddParameter(new AnimatorControllerParameter
                {
                    name = setup.speedParameter,
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = 1f,
                });
            }

            state.speedParameterActive = useParameter;
            state.speedParameter = useParameter ? setup.speedParameter : string.Empty;
            state.speed = useParameter ? 1f : setup.speed;
        }

        private static AnimatorOverrideController EnsureOverride(SpriteGenImportProfile profile, SpriteGenImportProfile.CharacterEntry entry)
        {
            if (entry.overrideController == null)
            {
                entry.overrideController = new AnimatorOverrideController(profile.sharedController);
                AssetDatabase.CreateAsset(entry.overrideController, $"{profile.clipFolder}/{entry.characterId}.overrideController");
                EditorUtility.SetDirty(profile);
            }
            else if (entry.overrideController.runtimeAnimatorController != profile.sharedController)
            {
                entry.overrideController.runtimeAnimatorController = profile.sharedController;
            }

            return entry.overrideController;
        }

        private static void ApplyOverrides(AnimatorOverrideController overrides, Dictionary<string, AnimationClip> baseClips, Dictionary<string, AnimationClip> clips)
        {
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrides.overridesCount);
            overrides.GetOverrides(pairs);

            for (int i = 0; i < pairs.Count; i++)
            {
                string stateName = baseClips.FirstOrDefault(b => b.Value == pairs[i].Key).Key;
                if (stateName != null && clips.TryGetValue(stateName, out AnimationClip clip))
                    pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, clip);
            }

            overrides.ApplyOverrides(pairs);
            EditorUtility.SetDirty(overrides);
        }

        // ---------- 工具 ----------

        private static string CopyIntoProject(string source, string assetPath)
        {
            if (!File.Exists(source))
                throw new FileNotFoundException($"找不到 sprite-gen 輸出：{source}");

            File.Copy(source, Path.GetFullPath(assetPath), true);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return assetPath;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;

            string parent = Path.GetDirectoryName(assetFolder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolder));
        }

        private static string DescribeImport(string id, SpriteGenManifest manifest, float ppu, int variantCount)
        {
            var text = new StringBuilder($"{id}: 格子 {manifest.CellWidth}x{manifest.CellHeight}，地面線 y={manifest.GroundLineFromTop}，");
            text.Append($"錨點 ({manifest.FootPivot.x:0.###}, {manifest.FootPivot.y:0.####})，格高 {manifest.CellHeight / ppu:0.##} 單位");
            if (variantCount > 0)
                text.Append($"，換色版 {variantCount} 張");
            foreach (SpriteGenManifest.State s in manifest.States)
                text.Append($"\n  {s.Name}: {s.Cells.Length} 格 {s.Fps}fps{(s.Loop ? " 循環" : "")}");
            return text.ToString();
        }
    }
}
