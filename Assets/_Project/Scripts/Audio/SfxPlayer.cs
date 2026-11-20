using System.Collections.Generic;
using UnityEngine;

namespace Cerberus.Audio
{
    /// <summary>
    /// 播放音效：場景裡放一個，任何地方呼叫 SfxPlayer.Play(SfxId.Hit)。
    /// 2D 音效（不分左右聲道），用一組輪流使用的 AudioSource，同時最多 voiceCount 個聲音。
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        private static SfxPlayer instance;

        [SerializeField] private SfxLibrary library;
        [SerializeField, Range(2, 32)] private int voiceCount = 12;

        private readonly List<AudioSource> voices = new List<AudioSource>();
        private readonly Dictionary<SfxId, float> lastPlayed = new Dictionary<SfxId, float>();
        private int nextVoice;

        private void Awake()
        {
            instance = this;
            for (int i = 0; i < voiceCount; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                voices.Add(source);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        /// <param name="pitch">基本音高（例如重攻擊用低一點的 0.85）</param>
        /// <param name="volumeScale">額外的音量倍率</param>
        public static void Play(SfxId id, float pitch = 1f, float volumeScale = 1f)
        {
            if (instance != null)
                instance.PlayInternal(id, pitch, volumeScale);
        }

        private void PlayInternal(SfxId id, float pitch, float volumeScale)
        {
            SfxLibrary.Entry entry = library != null ? library.Find(id) : null;
            if (entry == null || entry.clip == null)
                return;

            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < entry.minInterval)
                return;
            lastPlayed[id] = now;

            AudioSource source = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Count;

            source.Stop();
            source.clip = entry.clip;
            source.volume = library.masterVolume * entry.volume * volumeScale;
            source.pitch = pitch * (1f + Random.Range(-entry.pitchVariance, entry.pitchVariance));
            source.Play();
        }
    }
}
