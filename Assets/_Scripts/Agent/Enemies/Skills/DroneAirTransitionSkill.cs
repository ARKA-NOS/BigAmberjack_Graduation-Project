using _Scripts.Agent.Enemies.Bosses;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class DroneAirTransitionSkill : EnemyAttackSkill
    {
        [SerializeField] private DroneVisualModule visual;
        [SerializeField] private Transform muzzle;
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 10f;
        [SerializeField, Min(0.1f)] private float projectileRange = 16f;
        [SerializeField, Min(0f)] private float damage = 8f;
        [SerializeField, Range(0f, 160f)] private float shotgunAngle = 60f;

        private ProductionControlDrone _drone;
        private Vector2 _destination;
        private bool _enhanced;

        public bool DidArrive { get; private set; }
        public override bool ControlsMovement => true;
        protected override bool ShouldShowTelegraph => false;

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() &&
            Enemy is ProductionControlDrone drone && drone.TryPlanAirTransition(out _);

        protected override void PrepareAttack()
        {
            _drone = (ProductionControlDrone)Enemy;
            DidArrive = false;
            _enhanced = _drone.UseLastStandPatterns;
            _drone.StopMovement();
            Enemy.FaceTarget();
            _drone.TryPlanAirTransition(out _destination);
            visual.Play("Rise");
        }

        protected override void OnReleaseAttack()
        {
            CoolTimeStartTime = Time.time;
            _drone.MoveToAir(_destination);

            if (_enhanced && projectilePrefab != null && muzzle != null)
            {
                Collider2D targetBody = CastTarget.GetComponent<Collider2D>();
                Vector2 target = targetBody != null ? (Vector2)targetBody.bounds.center : (Vector2)CastTarget.position;
                Vector2 direction = (target - (Vector2)muzzle.position).normalized;

                if (direction == Vector2.zero) direction = Vector2.down;

                for (int i = 0; i < 5; i++)
                {
                    Vector2 shotDirection = Quaternion.Euler(0f, 0f, Mathf.Lerp(-shotgunAngle * 0.5f,
                        shotgunAngle * 0.5f, i / 4f)) * direction;
                    var projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);

                    SceneManager.MoveGameObjectToScene(projectile.gameObject, Enemy.gameObject.scene);
                    projectile.Speed = projectileSpeed;
                    projectile.Lifetime = projectileRange / Mathf.Max(0.1f, projectileSpeed);
                    projectile.Launch(Enemy, CastTarget, damage, shotDirection);
                }

                NotifyAttackPerformed(muzzle.position);
            }
        }

        public override void FixedUpdateAttack()
        {
            if (IsUsing && HasReleased) _drone.TickAirMovement();
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();

            if (!IsUsing || !HasReleased || _drone.IsMoving) return;

            DidArrive = true;
            EndSkill();
        }

        public override void OnAttackFinished() { }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            _drone?.StopMovement();
        }
    }
}
