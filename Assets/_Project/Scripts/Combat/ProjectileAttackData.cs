using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>射擊攻擊：進入判定階段的瞬間朝前方發射子彈，可一次發射多發做散射。</summary>
    [CreateAssetMenu(fileName = "NewProjectileAttack", menuName = "Cerberus/Attacks/Projectile Attack")]
    public class ProjectileAttackData : AttackData
    {
        [Header("Projectile")]
        public Projectile projectilePrefab;

        [Tooltip("發射點相對角色的位置，x 會依面向自動翻轉")]
        public Vector2 muzzleOffset = new Vector2(0.6f, 0.3f);

        [Min(0f)] public float projectileSpeed = 22f;

        [Tooltip("最遠飛行距離，超過就消失")]
        [Min(0f)] public float maxRange = 9f;

        [Min(1)] public int projectileCount = 1;

        [Tooltip("多發子彈時，最上與最下兩發之間的總夾角（度），其餘平均分配")]
        [Range(0f, 90f)] public float spreadAngle;

        /// <summary>第 index 發子彈的飛行方向，facing 為 1（右）或 -1（左）</summary>
        public Vector2 GetDirection(int index, int facing)
        {
            float angle = projectileCount > 1
                ? Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, index / (projectileCount - 1f))
                : 0f;

            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            direction.x *= facing;
            return direction;
        }

        public Vector2 GetMuzzlePosition(Vector2 origin, int facing) =>
            origin + new Vector2(muzzleOffset.x * facing, muzzleOffset.y);
    }
}
