using System.Collections.Generic;
using DevLib.BattleSystem;
using Lrw.Script.Agent;
using Lrw.Script.Agent.HealthSystem;
using UnityEngine;

namespace CTJ.Enemies.Bosses
{
    // 체력/사망/보상은 EnemyBase를 재사용하고, 패턴과 비행만 보스 쪽으로 분리합니다.
    public abstract class BossBase : EnemyBase
    {
        private readonly List<EnemyProjectile> _ownedProjectiles = new List<EnemyProjectile>();
        private Transform _observedTarget;
        private bool _hadTarget;
        public bool IsInvulnerable { get; protected set; }
        protected override bool UsesGroundMovement => false;
        protected BossFlightModule Flight { get; private set; }
        protected EnemySkillModule Skills { get; private set; }
        public bool HasCombatTarget
        {
            get
            {
                if (Target == null || !Target.gameObject.activeInHierarchy) return false;
                Agent agent = Target.GetComponentInParent<Agent>();
                return agent == null || agent.GetModule<IHealthModule>()?.IsDead != true;
            }
        }

        protected sealed override void InitializeAI()
        {
            Flight = GetModule<BossFlightModule>();
            Skills = GetModule<EnemySkillModule>();
            InitializeBoss();
            _observedTarget = Target;
            _hadTarget = HasCombatTarget;
        }

        protected sealed override void UpdateAI()
        {
            bool available = HasCombatTarget;
            if (_observedTarget != Target || (_hadTarget && !available))
            {
                InterruptPattern();
                OnCombatInterrupted();
                _hadTarget = false;
            }
            _observedTarget = Target;
            if (!available) return;
            _hadTarget = true;
            UpdateBoss();
        }

        protected sealed override void FixedUpdateAI()
        {
            if (HasCombatTarget) FixedUpdateBoss();
        }

        public override void SetTarget(Transform newTarget)
        {
            if (Target != newTarget)
            {
                InterruptPattern();
                OnCombatInterrupted();
            }
            base.SetTarget(newTarget);
            _observedTarget = newTarget;
            _hadTarget = HasCombatTarget;
        }

        public override void ApplyDamage(DamageData damageData, Vector2 point, Vector2 direction, Vector2 normal)
        {
            if (!IsInvulnerable) base.ApplyDamage(damageData, point, direction, normal);
        }

        public void TrackProjectile(EnemyProjectile projectile)
        {
            _ownedProjectiles.RemoveAll(item => item == null);
            if (projectile != null) _ownedProjectiles.Add(projectile);
        }

        protected void ClearProjectiles()
        {
            foreach (EnemyProjectile projectile in _ownedProjectiles)
            {
                if (projectile == null) continue;
                projectile.gameObject.SetActive(false); // Destroy가 반영되기 전에도 피해를 막습니다.
                Destroy(projectile.gameObject);
            }
            _ownedProjectiles.Clear();
        }

        private void InterruptPattern()
        {
            Skills?.StopAttack();
            Flight?.Stop();
            IsInvulnerable = false;
        }

        protected override void OnDisable()
        {
            InterruptPattern();
            ClearProjectiles();
            OnCombatInterrupted();
            base.OnDisable();
        }

        protected abstract void InitializeBoss();
        protected abstract void UpdateBoss();
        protected abstract void FixedUpdateBoss();
        protected abstract void OnCombatInterrupted();
    }
}
