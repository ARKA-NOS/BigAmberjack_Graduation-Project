using _Scripts.Agent.Enemies.Bosses;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class DroneSalvoSkill : EnemyAttackSkill
    {
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private DroneVisualModule visual;

        [SerializeField, Min(0.1f)] private float range = 16f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 15f;
        [SerializeField, Min(0.1f)] private float projectileScale = 1f;
        [SerializeField, Min(0f)] private float damage = 8f;
        [SerializeField, Min(0.1f)] private float firingDuration = 2.5f;
        [SerializeField, Min(1)] private int normalVolleys = 7;
        [SerializeField, Min(1)] private int lastStandVolleys = 12;
        [SerializeField, Range(0f, 80f)] private float lastStandSpreadAngle = 12f;
        [SerializeField, Min(0f)] private float verticalSpawnOffset = 0.3f;

        [SerializeField, Min(0f)] private float retreatDistance = 3f;
        [SerializeField, Min(0.1f)] private float retreatSpeed = 1.5f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.8f;

        private ProductionControlDrone _drone;
        private bool _enhanced;
        private int _volleyCount;
        private float _firingStartedAt;
        private float _recoveryUntil;

        public bool DidFire { get; private set; }
        public int FiredVolleys { get; private set; }
        public int ProjectilesFired { get; private set; }
        public float Range => range;
        public override bool ControlsMovement => true;
        protected override bool ShouldShowTelegraph => false;

        protected override bool CanUseSkillCondition() =>
            base.CanUseSkillCondition() && projectilePrefab != null && muzzle != null &&
            Mathf.Abs(Enemy.Target.position.x - Enemy.transform.position.x) <= range;

        protected override void PrepareAttack()
        {
            _drone = (ProductionControlDrone)Enemy;
            _enhanced = _drone.UseLastStandPatterns;
            _volleyCount = Mathf.Max(1, _enhanced ? lastStandVolleys : normalVolleys);
            DidFire = false;
            FiredVolleys = ProjectilesFired = 0;
            _drone.StopMovement();
            Enemy.FaceTarget();
            visual.Play("LaserCharge");
        }

        protected override void OnReleaseAttack()
        {
            _firingStartedAt = Time.time;
            Enemy.FaceTarget();
            float away = -Enemy.FacingDirection;

            if (_drone.TryPlanGroundMove(away, retreatDistance, out Vector2 destination))
                _drone.MoveGround(destination, retreatSpeed);

            visual.Play("Laser");
            FireVolley();
        }

        private void FireVolley()
        {
            Enemy.FaceTarget();
            Vector2 direction = Vector2.right * Enemy.FacingDirection;
            Vector3 origin = muzzle.position + Vector3.up * Random.Range(-verticalSpawnOffset, verticalSpawnOffset);
            int count = _enhanced ? 3 : 1;

            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : (i - 1) * lastStandSpreadAngle;
                Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;
                var projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);

                SceneManager.MoveGameObjectToScene(projectile.gameObject, Enemy.gameObject.scene);
                projectile.transform.localScale *= Mathf.Max(0.1f, projectileScale);
                projectile.Speed = projectileSpeed;
                projectile.Lifetime = range / Mathf.Max(0.1f, projectileSpeed);
                projectile.Launch(Enemy, CastTarget, damage, shotDirection);
                ProjectilesFired++;
            }

            FiredVolleys++;
            DidFire = true;
            CoolTimeStartTime = Time.time;
            NotifyAttackPerformed(origin);
        }

        public override void FixedUpdateAttack()
        {
            if (IsUsing && HasReleased && !IsRecovering) _drone.TickGroundMovement();
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();

            if (!IsUsing || !HasReleased) return;

            if (IsRecovering)
            {
                if (Time.time >= _recoveryUntil) EndSkill();

                return;
            }

            float elapsed = Time.time - _firingStartedAt;
            float interval = firingDuration / Mathf.Max(1, _volleyCount - 1);

            while (FiredVolleys < _volleyCount && elapsed >= FiredVolleys * interval) FireVolley();

            if (elapsed < firingDuration)
            {
                if (visual.HasFinished("Laser")) visual.Play("Laser");

                return;
            }

            if (!visual.HasFinished("Laser")) return;

            _drone.StopMovement();
            visual.Play("Idle");
            BeginRecovery();
            _recoveryUntil = Time.time + recoveryTime;
        }

        public override void OnAttackFinished() { }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            _drone?.StopMovement();
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            _drone?.StopMovement();
            visual?.SetGunFlash(false);
        }
    }
}
