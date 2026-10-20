using Lrw.Script.Agent.StatSystem;
using DevLib.ModuleSystem;
using CTJ.Enemies.Skills;
using UnityEngine;

namespace CTJ.Enemies.Bosses
{
    public sealed class BossRadialVolleySkill : EnemyAttackSkill
    {
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private StatData damageStat;
        [SerializeField, Min(1)] private int projectileCount = 12;
        [SerializeField, Min(0.1f)] private float warningRadius = 1.4f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.5f;
        private Stat _damage;
        private float _endsAt;
        public Vector2 LockedOrigin { get; private set; }
        public bool DidFire { get; private set; }

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            if (damageStat != null && owner.TryGetModule(out IStatModule stats)) _damage = stats.GetStat(damageStat);
        }

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() &&
            Enemy is BossBase && projectilePrefab != null && firePoint != null;

        protected override void PrepareAttack()
        {
            LockedOrigin = firePoint.position;
            DidFire = false;
        }

        protected override void UpdateTelegraph() => Telegraph.SetBounds(
            new Bounds(LockedOrigin, Vector2.one * (Mathf.Max(0.1f, warningRadius) * 2f)));

        protected override void OnReleaseAttack()
        {
            Telegraph?.Hide();
            BossBase boss = (BossBase)Enemy;
            int count = Mathf.Max(1, projectileCount);
            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * i / count;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                EnemyProjectile projectile = Instantiate(projectilePrefab, LockedOrigin, Quaternion.identity);
                boss.TrackProjectile(projectile);
                projectile.Launch(boss, CastTarget, Mathf.Max(0f, _damage?.Value ?? 30f), direction);
            }
            DidFire = true;
            _endsAt = Time.time + Mathf.Max(0f, recoveryTime);
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (IsUsing && HasReleased && Time.time >= _endsAt) EndSkill();
        }
    }
}
