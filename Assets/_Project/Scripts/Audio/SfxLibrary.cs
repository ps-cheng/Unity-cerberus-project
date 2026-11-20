using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cerberus.Audio
{
    public enum SfxId
    {
        CaneSwing,
        Gunshot,
        Claw,
        Hit,
        EnemyExplode,
        HeadSwitch,
        LaserCharge,
        LaserFire,
        Parry,
        Hurt,
        Coin,
        UiClick
    }

    /// <summary>
    /// 所有音效和音量集中在這一個資產（Assets/_Project/Data/Audio/SfxLibrary.asset）。
    /// 調整音量只改這裡：masterVolume 是總音量，每個音效再各自乘上 volume。
    /// </summary>
    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "Cerberus/Sfx Library")]
    public class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public SfxId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.8f;
            [Tooltip("每次播放時音高隨機上下浮動的比例，避免重複聽起來太機械")]
            [Range(0f, 0.3f)] public float pitchVariance = 0.05f;
            [Tooltip("同一個音效最短間隔（秒），避免同一幀疊加好幾次變得很大聲")]
            [Min(0f)] public float minInterval = 0.03f;
        }

        [Range(0f, 1f)] public float masterVolume = 0.8f;
        public List<Entry> entries = new List<Entry>();

        public Entry Find(SfxId id) => entries.Find(entry => entry.id == id);
    }
}
