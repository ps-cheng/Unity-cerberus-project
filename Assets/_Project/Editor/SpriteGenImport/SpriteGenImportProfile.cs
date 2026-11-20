using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace Cerberus.EditorTools.SpriteGenImport
{
    /// <summary>
    /// 一次匯入要用的所有設定：sprite-gen 輸出在哪、匯入到哪、PPU、共用的 Animator Controller 和每個角色的 Override。
    /// 存成資產，之後重新生圖只要按一次「匯入全部」。
    /// </summary>
    [CreateAssetMenu(menuName = "Cerberus/Sprite-Gen Import Profile", fileName = "SpriteGenImportProfile")]
    public sealed class SpriteGenImportProfile : ScriptableObject
    {
        /// <summary>共用 Controller 裡一個狀態的播放速度設定</summary>
        [Serializable]
        public sealed class StateSetup
        {
            [Tooltip("跟 manifest 裡的狀態名稱一樣，例如 idle、run、light-attack")]
            public string stateName;
            [Tooltip("用哪個 float 參數控制播放速度（留空就用下面的固定速度）。不存在的參數會自動建立，預設值 1")]
            public string speedParameter;
            [Tooltip("沒有速度參數時的固定播放速度。0 = 由程式指定要停在哪一格（例如跳躍）")]
            public float speed = 1f;
        }

        /// <summary>一個角色（一顆頭）</summary>
        [Serializable]
        public sealed class CharacterEntry
        {
            [Tooltip("sprite-gen 的 character-id，也是 runs 底下的資料夾名稱，例如 Cer")]
            public string characterId;
            [Tooltip("這個角色的 Override Controller；留空會自動建立")]
            public AnimatorOverrideController overrideController;
            [Tooltip("格子位置完全相同的換色版圖集（例如狂暴化），只切圖、不建 clip")]
            public List<VariantEntry> variants = new List<VariantEntry>();
        }

        [Serializable]
        public sealed class VariantEntry
        {
            [Tooltip("加在角色名後面當作 sprite 名稱的前綴，例如 Berserk → RusBerserk_idle_0")]
            public string suffix;
            [Tooltip("相對於角色 run 資料夾的圖集路徑，例如 variants/berserk/sprite-sheet-alpha.png")]
            public string relativeSheetPath;
        }

        [Header("來源")]
        [Tooltip("sprite-gen 的 runs 資料夾（專案外的絕對路徑）")]
        public string runsRoot = @"D:\Codex\sprite-work\runs";

        [Header("目的地")]
        [Tooltip("圖集和 manifest 複製到 <這裡>/<角色>/")]
        public string atlasFolder = "Assets/Art/Characters";
        [Tooltip("Animation Clip 建在 <這裡>/<角色>/，共用的 Base clip 建在這裡")]
        public string clipFolder = "Assets/_Project/Art/Animations/Player";

        [Header("貼圖")]
        [Tooltip("所有角色共用同一個值，三種型態才會一樣大")]
        [Min(1f)] public float pixelsPerUnit = 128f;

        [Header("Animator")]
        [Tooltip("三顆頭共用的 Controller；留空會在 clipFolder 建立 Player.controller")]
        public AnimatorController sharedController;
        public List<StateSetup> states = new List<StateSetup>
        {
            new StateSetup { stateName = "idle", speedParameter = "IdleSpeed" },
            new StateSetup { stateName = "run", speedParameter = "RunSpeed" },
            new StateSetup { stateName = "light-attack", speedParameter = "AttackSpeed" },
            new StateSetup { stateName = "heavy-attack", speedParameter = "AttackSpeed" },
        };

        [Header("角色")]
        public List<CharacterEntry> characters = new List<CharacterEntry>();
    }
}
