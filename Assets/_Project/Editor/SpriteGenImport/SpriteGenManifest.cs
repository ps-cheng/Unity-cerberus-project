using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Cerberus.EditorTools.SpriteGenImport
{
    /// <summary>
    /// sprite-gen 產出的 <c>manifest.json</c> 裡匯入 Unity 需要的部分。
    /// manifest 的 rows 是「狀態名稱 → 資料」的字典，JsonUtility 讀不了，所以用 Newtonsoft 解析。
    /// </summary>
    public sealed class SpriteGenManifest
    {
        /// <summary>一個動畫狀態（圖集裡的一列）</summary>
        public sealed class State
        {
            public string Name;
            public bool Loop;
            public int Fps;
            /// <summary>每一格在圖集裡的位置，座標原點在左上（manifest 的寫法）</summary>
            public RectInt[] Cells;
            /// <summary>每一格停留的毫秒數，長度與 Cells 相同</summary>
            public int[] DurationsMs;
        }

        public string CharacterId { get; private set; }
        public int SheetWidth { get; private set; }
        public int SheetHeight { get; private set; }
        public int CellWidth { get; private set; }
        public int CellHeight { get; private set; }

        /// <summary>
        /// 地面線，從格子上緣往下數的像素列（Cerberus 的圖集是 231）。
        /// sprite-gen 把角色腳底放在「格子底部往上留 safe_margin_y」的位置，所以地面線 = 格高 - safe_margin_y - 1。
        /// </summary>
        public int GroundLineFromTop { get; private set; }

        /// <summary>依圖集由上到下的順序排列的狀態</summary>
        public IReadOnlyList<State> States { get; private set; }

        /// <summary>腳底錨點，Unity 的 normalized pivot（原點在左下）</summary>
        public Vector2 FootPivot => new Vector2(0.5f, (float)(CellHeight - GroundLineFromTop) / CellHeight);

        public static SpriteGenManifest Load(string manifestPath)
        {
            JObject root = JObject.Parse(File.ReadAllText(manifestPath));
            JObject layout = Require<JObject>(root, "frame_layout", manifestPath);
            JObject animation = Require<JObject>(root, "animation", manifestPath);
            JObject layoutRows = Require<JObject>(layout, "rows", manifestPath);
            JObject animationRows = Require<JObject>(animation, "rows", manifestPath);

            var manifest = new SpriteGenManifest
            {
                CharacterId = (string)root["characterId"],
                SheetWidth = (int)layout["sheetWidth"],
                SheetHeight = (int)layout["sheetHeight"],
                CellWidth = (int)layout["cellWidth"],
                CellHeight = (int)layout["cellHeight"],
            };

            int safeMarginY = (int?)root["cell"]?["safe_margin_y"] ?? 0;
            manifest.GroundLineFromTop = manifest.CellHeight - safeMarginY - 1;

            var states = new List<(int row, State state)>();
            foreach (JProperty property in animationRows.Properties())
            {
                JObject info = (JObject)property.Value;
                JArray cells = layoutRows[property.Name] as JArray
                    ?? throw new InvalidDataException($"{manifestPath}: frame_layout 裡沒有狀態 {property.Name}");

                var state = new State
                {
                    Name = property.Name,
                    Loop = (bool)info["loop"],
                    Fps = (int)info["fps"],
                    Cells = cells.Select(c => new RectInt((int)c["x"], (int)c["y"], (int)c["w"], (int)c["h"])).ToArray(),
                };

                // durations_ms 是選填，沒有的話每格平均 1/fps
                JArray durations = info["durations_ms"] as JArray;
                state.DurationsMs = durations != null && durations.Count == state.Cells.Length
                    ? durations.Select(d => (int)d).ToArray()
                    : Enumerable.Repeat(Mathf.RoundToInt(1000f / state.Fps), state.Cells.Length).ToArray();

                states.Add(((int)info["row"], state));
            }

            manifest.States = states.OrderBy(s => s.row).Select(s => s.state).ToList();
            return manifest;
        }

        private static T Require<T>(JObject parent, string key, string path) where T : JToken
            => parent[key] as T ?? throw new InvalidDataException($"{path}: 缺少 {key}");
    }
}
