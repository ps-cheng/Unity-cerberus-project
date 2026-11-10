using System;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies.Boss
{
    /// <summary>
    /// Boss 狀態的共同介面。每個狀態是獨立的類別，參數直接寫在各自的 public 欄位上，
    /// 在 Inspector 會以 BossController 底下的一個個折疊區塊出現，方便單獨調整或抽換。
    /// </summary>
    [Serializable]
    public abstract class BossState
    {
        [NonSerialized] protected BossController boss;

        /// <summary>進入此狀態後經過的時間（Boss 硬直時不會增加）</summary>
        protected float Timer { get; private set; }

        /// <summary>顯示在敵人頭上的除錯文字</summary>
        public abstract string DisplayName { get; }

        public void Initialize(BossController owner) => boss = owner;

        public void Enter()
        {
            Timer = 0f;
            OnEnter();
        }

        public void Tick(float deltaTime)
        {
            Timer += deltaTime;
            OnTick(deltaTime);
        }

        public virtual void Exit() { }

        protected virtual void OnEnter() { }

        protected abstract void OnTick(float deltaTime);

        /// <summary>受擊時的防禦判定，預設全部吃下</summary>
        public virtual DamageResult ResolveIncomingHit(DamageInfo info) => DamageResult.Hit;

        public virtual Color GetBodyColor(Color baseColor) => baseColor;

        /// <summary>每幀更新外觀（武器角度、盾、身體縮放）。呼叫前 Boss 已把外觀重設回待機姿勢</summary>
        public virtual void UpdateVisuals() { }

        /// <summary>目前的攻擊範圍，給除錯用的範圍視覺化</summary>
        public virtual bool TryGetZone(out Vector2 center, out Vector2 size, out ActionPhase phase)
        {
            center = Vector2.zero;
            size = Vector2.zero;
            phase = ActionPhase.None;
            return false;
        }
    }
}
