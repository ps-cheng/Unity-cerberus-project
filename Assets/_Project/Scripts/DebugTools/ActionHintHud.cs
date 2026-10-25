using System.Collections.Generic;
using Cerberus.Combat;
using Cerberus.Player;
using UnityEngine;

namespace Cerberus.DebugTools
{
    /// <summary>
    /// 原型期的除錯 HUD（IMGUI，不需要任何 UI 物件）：
    /// 左上角顯示操作說明與目前狀態，右上角列出最近觸發的動作，角色頭上顯示目前招式。
    /// </summary>
    public class ActionHintHud : MonoBehaviour
    {
        private struct ActionLogEntry
        {
            public string message;
            public float time;
        }

        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private Vector3 labelWorldOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private int maxLogEntries = 6;
        [SerializeField] private float logLifetime = 2.5f;

        private readonly List<ActionLogEntry> actionLog = new List<ActionLogEntry>();
        private Camera mainCamera;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle floatingLabelStyle;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            movement.Jumped += HandleJumped;
            combat.ActionStarted += HandleActionStarted;
            combat.TargetHit += HandleTargetHit;
        }

        private void OnDisable()
        {
            movement.Jumped -= HandleJumped;
            combat.ActionStarted -= HandleActionStarted;
            combat.TargetHit -= HandleTargetHit;
        }

        private void HandleJumped() => AddLog("Jump");
        private void HandleActionStarted(CombatAction action) => AddLog(GetActionName(action));
        private void HandleTargetHit(Collider2D target) => AddLog($"Hit -> {target.name}");

        private void AddLog(string message)
        {
            actionLog.Insert(0, new ActionLogEntry { message = message, time = Time.time });

            if (actionLog.Count > maxLogEntries)
                actionLog.RemoveAt(actionLog.Count - 1);
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawStatusPanel();
            DrawActionLog();
            DrawFloatingLabel();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
                return;

            panelStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 8, 8) };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
            floatingLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
        }

        private void DrawStatusPanel()
        {
            GUILayout.BeginArea(new Rect(10, 10, 360, 270), panelStyle);
            GUILayout.Label("<b>Controls</b>", labelStyle);
            GUILayout.Label("A / D / Arrows   Move", labelStyle);
            GUILayout.Label("Space            Jump (hold = higher)", labelStyle);
            GUILayout.Label("J / LMB          Light Attack", labelStyle);
            GUILayout.Label("K / RMB          Heavy Attack", labelStyle);
            GUILayout.Label("L (hold)          Block", labelStyle);
            GUILayout.Space(6);
            GUILayout.Label($"<b>State</b>  {(movement.IsGrounded ? "Grounded" : "Airborne")}   Vel {movement.Velocity.x:0.0}, {movement.Velocity.y:0.0}", labelStyle);
            GUILayout.Label($"<b>Action</b> {GetActionName(combat.CurrentAction)}  {GetPhaseText()}", labelStyle);
            GUILayout.EndArea();
        }

        private void DrawActionLog()
        {
            float y = 10f;

            foreach (ActionLogEntry entry in actionLog)
            {
                float age = Time.time - entry.time;
                if (age > logLifetime)
                    continue;

                // 越舊越透明
                GUI.color = new Color(1f, 1f, 1f, 1f - age / logLifetime);
                GUI.Label(new Rect(Screen.width - 230, y, 220, 26), $"> {entry.message}", labelStyle);
                y += 24f;
            }

            GUI.color = Color.white;
        }

        private void DrawFloatingLabel()
        {
            if (!combat.IsBusy || mainCamera == null)
                return;

            Vector3 screenPoint = mainCamera.WorldToScreenPoint(movement.transform.position + labelWorldOffset);
            // IMGUI 的 y 軸由上往下，需反轉
            Rect rect = new Rect(screenPoint.x - 150, Screen.height - screenPoint.y - 25, 300, 50);
            GUI.Label(rect, $"{GetActionName(combat.CurrentAction)}\n<size=13>{GetPhaseText()}</size>", floatingLabelStyle);
        }

        private string GetPhaseText()
        {
            if (combat.CurrentPhase == ActionPhase.None || combat.IsBlocking)
                return string.Empty;

            string phaseName = combat.CurrentPhase switch
            {
                ActionPhase.Startup => "<color=yellow>Startup</color>",
                ActionPhase.Active => "<color=red>Active</color>",
                _ => "<color=silver>Recovery</color>"
            };

            return $"[{phaseName} {combat.PhaseProgress:P0}]";
        }

        private static string GetActionName(CombatAction action) => action switch
        {
            CombatAction.LightAttack => "Light Attack",
            CombatAction.HeavyAttack => "Heavy Attack",
            CombatAction.Block => "Block",
            _ => "-"
        };
    }
}
