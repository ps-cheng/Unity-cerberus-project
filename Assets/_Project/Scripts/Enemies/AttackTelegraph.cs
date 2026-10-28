using System;
using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 敵人出招前的顏色提示，所有敵人共用同一套規則讓玩家好判讀：
    /// 前搖期間黃色閃爍且越來越快，最後一小段停止閃爍轉白，代表「現在就是格檔時機」。
    /// </summary>
    [Serializable]
    public class AttackTelegraph
    {
        public Color warningColor = new Color(1f, 0.85f, 0.2f);

        [Tooltip("閃爍頻率從 Start 漸增到 End，越接近出招閃得越快")]
        public float blinkStartFrequency = 4f;
        public float blinkEndFrequency = 14f;

        [Tooltip("出招前最後這段時間停止閃爍並轉白，作為格檔時機的提示。" +
                 "略長於人的反應時間（約 0.2 秒），看到再按剛好落在完美格檔的判定內")]
        public float cueDuration = 0.25f;
        public Color cueColor = Color.white;

        public bool IsCueActive(float elapsed, float windupDuration) => elapsed >= windupDuration - cueDuration;

        public Color Evaluate(Color baseColor, float elapsed, float windupDuration)
        {
            if (IsCueActive(elapsed, windupDuration))
                return cueColor;

            float progress = windupDuration > 0f ? Mathf.Clamp01(elapsed / windupDuration) : 1f;
            float frequency = Mathf.Lerp(blinkStartFrequency, blinkEndFrequency, progress);
            bool isHighlighted = Mathf.Repeat(elapsed * frequency, 1f) < 0.5f;
            return isHighlighted ? warningColor : baseColor;
        }
    }
}
