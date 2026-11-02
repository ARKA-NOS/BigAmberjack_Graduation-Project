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
        [SerializeField, Min(0f)] private float recoveryTime = 0.5f;
        private Stat _damage;
        private float _endsAt;
        private int _castCount;
        private float _castSpeed;
        private float _castLifetime;
        private float _castWidth;
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
            // 경고와 발사가 같은 개수/방향/거리를 쓰도록 준비 시점의 값을 기억합니다.
            _castCount = Mathf.Max(1, projectileCount);
            _castSpeed = projectilePrefab.Speed;
            _castLifetime = projectilePrefab.Lifetime;
            _castWidth = projectilePrefab.Diameter;
            DidFire = false;
        }

        protected override void UpdateTelegraph() => Telegraph.SetRadialLanes(
            LockedOrigin, _castCount, _castSpeed * _castLifetime, _castWidth);

        protected override void OnReleaseAttack()
        {
            Telegraph?.Hide();
            BossBase boss = (BossBase)Enemy;
            int count = _castCount;
            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * i / count;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                EnemyProjectile projectile = Instantiate(projectilePrefab, LockedOrigin, Quaternion.identity);
                projectile.Speed = _castSpeed;
                projectile.Lifetime = _castLifetime;
                boss.TrackProjectile(projectile);
                projectile.Launch(boss, CastTarget, Mathf.Max(0f, _damage?.Value ?? 30f), direction);
            }
            DidFire = true;
            _endsAt = Time.time + Mathf.Max(0f, recoveryTime);
            BeginRecovery();
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (IsUsing && HasReleased && Time.time >= _endsAt) EndSkill();
        }
    }
}
