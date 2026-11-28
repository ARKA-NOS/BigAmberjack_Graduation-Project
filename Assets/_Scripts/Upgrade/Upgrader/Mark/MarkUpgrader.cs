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
        [SerializeField] private float physicalRatio = 1f;
        [SerializeField] private StatData magicPower;
        [SerializeField] private float magicRatio = 1f;

        private Stat _physicalStat;
        private Stat _magicStat;
        private int _hitCount;

        public int Stack { get; private set; }

        public override void InitUpgrader(ModuleOwner owner)
        {
            base.InitUpgrader(owner);

            if (burstCaster != null)
                burstCaster.InitCaster(owner);

            if (!owner.TryGetModule(out IStatModule statModule)) return;

            statModule.TryGetStat(physicalPower, out _physicalStat);
            statModule.TryGetStat(magicPower, out _magicStat);
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

            if (Stack >= maxStack)
                Burst();
        }

        private void Burst()
        {
            Stack = 0;

            if (burstCaster == null) return;

            burstCaster.CastDamage(GetBurstDamage(), Vector2.zero, 0f);
        }

        private float GetBurstDamage()
        {
            float physical = _physicalStat != null ? _physicalStat.Value * physicalRatio : 0f;
            float magic = _magicStat != null ? _magicStat.Value * magicRatio : 0f;
            return physical + magic;
        }
    }
}
