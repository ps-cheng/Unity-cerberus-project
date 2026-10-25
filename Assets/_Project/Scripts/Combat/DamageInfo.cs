using UnityEngine;

namespace Cerberus.Combat
{
    public struct DamageInfo
    {
        public int amount;
        // 用來判斷受擊方向（擊退方向、格檔是否來自正面）
        public Vector2 sourcePosition;
        public float knockbackForce;

        public DamageInfo(int amount, Vector2 sourcePosition, float knockbackForce)
        {
            this.amount = amount;
            this.sourcePosition = sourcePosition;
            this.knockbackForce = knockbackForce;
        }
    }
}
