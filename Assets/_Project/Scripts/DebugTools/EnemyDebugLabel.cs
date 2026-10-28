using Cerberus.Enemies;
using UnityEngine;

namespace Cerberus.DebugTools
{
    /// <summary>
    /// 在敵人頭上顯示目前狀態與血量。
    /// </summary>
    public class EnemyDebugLabel : MonoBehaviour
    {
        [SerializeField] private EnemyBase enemy;
        [SerializeField] private Vector3 labelWorldOffset = new Vector3(0f, 1f, 0f);

        private Camera mainCamera;
        private GUIStyle labelStyle;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void OnGUI()
        {
            if (mainCamera == null)
                return;

            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            Vector3 screenPoint = mainCamera.WorldToScreenPoint(enemy.transform.position + labelWorldOffset);
            Rect rect = new Rect(screenPoint.x - 100, Screen.height - screenPoint.y - 20, 200, 40);
            string stateColor = enemy.HasTarget ? "#FF6060" : "#A0FFA0";
            string stateName = enemy.Health.IsDead ? "Dead" : enemy.StateName;
            GUI.Label(rect, $"<color={stateColor}>{stateName}</color>\nHP {enemy.Health.CurrentHealth}/{enemy.Health.MaxHealth}", labelStyle);
        }
    }
}
