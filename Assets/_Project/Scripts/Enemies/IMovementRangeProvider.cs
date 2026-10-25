using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 提供敵人活動範圍的折線點（世界座標），給除錯視覺化使用。
    /// </summary>
    public interface IMovementRangeProvider
    {
        Vector3[] GetMovementRangePoints();
    }
}
