using _Scripts._Core._Debug;
using _Scripts._Core._EventSystem;
using _Scripts.Agent.Players.Skills;
using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.BattleSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Upgrade.Upgrader.Mark
{
    public class MarkUpgrader : AbstractUpgraderSystem
    {
        [Header("Stack")]
        [SerializeField, Min(1)] private int hitsPerStack = 5;
        [SerializeField, Min(1)] private int maxStack = 5;

        [Header("Burst Damage")]
        [SerializeField] private AbstractDamageCaster burstCaster;
        [SerializeField] private StatData physicalPower;
        [SerializeField] private float physicalRatio = 0.3f;

        private Stat _physicalStat;
        private int _hitCount;

        public int Stack { get; private set; }

        public override void InitUpgrader(ModuleOwner owner)
        {
            base.InitUpgrader(owner);

            if (burstCaster != null)
                burstCaster.InitCaster(owner);

            if (!owner.TryGetModule(out IStatModule statModule)) return;

            statModule.TryGetStat(physicalPower, out _physicalStat);
        }

        protected override void OnActivate()
        {
            EventBus<MeleeAttackHitEvent>.Subscribe(Owner, OnMeleeAttackHit);
        }

        protected override void OnDeactivate()
        {
            EventBus<MeleeAttackHitEvent>.UnSubscribe(Owner, OnMeleeAttackHit);
            _hitCount = 0;
            Stack = 0;
        }

        private void OnMeleeAttackHit(MeleeAttackHitEvent evt)
        {
            _hitCount++;
            if (_hitCount < hitsPerStack) return;

            _hitCount = 0;
            Stack++;
            FDebug.Log($"[Mark] 표식 {Stack}/{maxStack}", this);

            if (Stack >= maxStack)
                Burst();
        }

        private void Burst()
        {
            Stack = 0;

            if (burstCaster == null) return;

            float damage = GetBurstDamage();
            bool isHit = burstCaster.CastDamage(damage, Vector2.zero, 0f);
            FDebug.Log($"[Mark] 표식 발동 피해 {damage:F2} (물리 {(_physicalStat != null ? _physicalStat.Value : 0f):F2} x {physicalRatio:F2}), 적중 {isHit}", this);
        }

        private float GetBurstDamage()
            => _physicalStat != null ? _physicalStat.Value * physicalRatio : 0f;
    }
}
