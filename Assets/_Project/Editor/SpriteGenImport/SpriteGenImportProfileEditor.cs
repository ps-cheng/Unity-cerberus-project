using UnityEditor;
using UnityEngine;

namespace Cerberus.EditorTools.SpriteGenImport
{
    /// <summary>Import Profile 的 Inspector：設定欄位下面加上匯入按鈕</summary>
    [CustomEditor(typeof(SpriteGenImportProfile))]
    public sealed class SpriteGenImportProfileEditor : Editor
    {
        private const string MenuPath = "Tools/Cerberus/Import Sprite-Gen Atlases";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var profile = (SpriteGenImportProfile)target;
            EditorGUILayout.Space();

            if (GUILayout.Button("匯入全部角色", GUILayout.Height(28)))
                SpriteGenAtlasImporter.ImportAll(profile);

            foreach (SpriteGenImportProfile.CharacterEntry entry in profile.characters)
            {
                if (!string.IsNullOrEmpty(entry.characterId) && GUILayout.Button($"只匯入 {entry.characterId}"))
                {
                    SpriteGenAtlasImporter.ImportCharacter(profile, entry);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        /// <summary>從選單匯入：用專案裡找到的每一個 Import Profile</summary>
        [MenuItem(MenuPath)]
        private static void ImportFromMenu()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(SpriteGenImportProfile)}");
            if (guids.Length == 0)
            {
                Debug.LogWarning("找不到 Sprite-Gen Import Profile。用 Create > Cerberus > Sprite-Gen Import Profile 建立一個。");
                return;
            }

            foreach (string guid in guids)
                SpriteGenAtlasImporter.ImportAll(AssetDatabase.LoadAssetAtPath<SpriteGenImportProfile>(AssetDatabase.GUIDToAssetPath(guid)));
        }
    }
}
