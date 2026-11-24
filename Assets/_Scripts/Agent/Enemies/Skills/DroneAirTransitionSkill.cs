using _Scripts.Agent.Enemies.Bosses;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class DroneAirTransitionSkill : EnemyAttackSkill
    {
        [SerializeField] private DroneVisualModule visual;
        [SerializeField] private Transform muzzle;
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 10f;
        [SerializeField, Min(0.1f)] private float projectileRange = 16f;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Range(0f, 160f)] private float shotgunAngle = 60f;
        [SerializeField, Min(0.01f)] private float transformationDuration = 0.55f;
        [Tooltip("사격 모션이 끝난 뒤 비행 기본 자세로 쉬는 시간입니다. 사격 전에는 대기하지 않습니다.")]
        [SerializeField, FormerlySerializedAs("aimedShotDelay"), Min(0f)] private float postAttackDelay = 0.15f;
        [SerializeField, Min(0.01f)] private float shotDuration = 0.41f;
        [SerializeField, Min(0.1f)] private float lastStandSpeedMultiplier = 1.2f;
        [Tooltip("기본 상승만 GIF처럼 조금 여유 있게 진행합니다. 강화·발악 진입에는 적용하지 않습니다.")]
        [SerializeField, Min(0.1f)] private float normalAscentSpeedMultiplier = 0.65f;

        private ProductionControlDrone _drone;
        private Vector2 _destination;
        private bool _enhanced, _entry, _moving, _fired, _returnedToIdle;
        private float _moveAt, _shotEndsAt, _recoverUntil, _speedMultiplier;

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
            _entry = _drone.IsLastStandEntry;
            _speedMultiplier = _enhanced ? Mathf.Max(0.1f, lastStandSpeedMultiplier) : 1f;
            _moving = _fired = _returnedToIdle = false;
            _moveAt = Time.time + transformationDuration / _speedMultiplier;
            _drone.StopMovement();
            Enemy.FaceTarget();
            _drone.TryPlanAirTransition(out _destination);
            visual.Play("Rise", _speedMultiplier);
        }

        protected override void OnReleaseAttack() => CoolTimeStartTime = Time.time;

        private void Fire(int count)
        {
            _fired = true;
            _shotEndsAt = Time.time + shotDuration / _speedMultiplier;
            _recoverUntil = _shotEndsAt + postAttackDelay / _speedMultiplier;
            Enemy.FaceTarget();
            visual.Play("AirShot", _speedMultiplier);
            if (projectilePrefab == null || muzzle == null) return;

            Collider2D targetBody = CastTarget.GetComponent<Collider2D>();
            Vector2 target = targetBody != null ? (Vector2)targetBody.bounds.center : (Vector2)CastTarget.position;
            Vector2 direction = (target - (Vector2)muzzle.position).normalized;
            if (direction == Vector2.zero) direction = Vector2.down;

            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : Mathf.Lerp(-shotgunAngle * 0.5f, shotgunAngle * 0.5f, i / (float)(count - 1));
                var projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(projectile.gameObject, Enemy.gameObject.scene);
                projectile.Speed = projectileSpeed;
                projectile.Lifetime = projectileRange / Mathf.Max(0.1f, projectileSpeed);
                projectile.Launch(Enemy, CastTarget, damage, Quaternion.Euler(0f, 0f, angle) * direction);
            }

            CoolTimeStartTime = Time.time;
            NotifyAttackPerformed(muzzle.position);
        }

        public override void FixedUpdateAttack()
        {
            if (IsUsing && HasReleased && _moving) _drone.TickAirMovement();
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (!IsUsing || !HasReleased) return;

            if (!_moving)
            {
                if (Time.time < _moveAt) return;
                _moving = true;
                visual.Play("AirIdle");
                float movementSpeed = _speedMultiplier * (_enhanced || _entry ? 1f : normalAscentSpeedMultiplier);
                _drone.MoveToAir(_destination, movementSpeed);
                if (_enhanced && !_entry) Fire(5);
            }

            if (_fired && !_returnedToIdle && Time.time >= _shotEndsAt)
            {
                _returnedToIdle = true;
                visual.Play("AirIdle");
                BeginRecovery();
            }

            if (_drone.IsMoving) return;

            if (!_fired && !_entry)
                Fire(1);

            if (_fired && (!_returnedToIdle || Time.time < _recoverUntil)) return;
            DidArrive = true;
            EndSkill();
        }

        public override void OnAttackFinished() { }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            visual?.Play("AirIdle");
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            _drone?.StopMovement();
            visual?.ResetPlaybackSpeed();
        }
    }
}
