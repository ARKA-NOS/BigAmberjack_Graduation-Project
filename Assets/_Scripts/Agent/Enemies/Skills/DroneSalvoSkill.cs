using _Scripts.Agent.Enemies.Bosses;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class DroneSalvoSkill : EnemyAttackSkill
    {
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform rearMuzzle;
        [SerializeField] private DroneVisualModule visual;

        [SerializeField, Min(0.1f)] private float range = 16f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 15f;
        [SerializeField, Min(0.1f)] private float projectileScale = 1f;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0.01f)] private float normalShotInterval = 0.32f;
        [SerializeField, Min(0.01f)] private float lastStandShotInterval = 0.144f;
        [SerializeField, Min(0f)] private float chargeEndTime = 0.55f;
        [SerializeField, Min(0f)] private float firstShotTime = 0.58f;
        [SerializeField, Min(0f)] private float lastStandFirstShotTime = 0.48f;
        [SerializeField, Min(0.1f)] private float firingAnimationSpeed = 1.2f;
        [SerializeField, Min(0.1f)] private float lastStandFiringAnimationSpeed = 1.2f;
        [SerializeField, Min(1)] private int normalVolleys = 7;
        [SerializeField, Min(1)] private int lastStandVolleys = 12;
        [SerializeField, Range(0f, 80f)] private float lastStandSpreadAngle = 12f;
        [SerializeField, Min(0f)] private float verticalSpawnOffset = 0.3f;

        [Tooltip("사격 중 유지할 플레이어와의 가로 거리입니다. 발사 방향은 바꾸지 않고 앞뒤로 이동합니다.")]
        [SerializeField, FormerlySerializedAs("retreatDistance"), Min(0f)] private float preferredDistance = 3f;
        [SerializeField, Min(0.01f)] private float distanceTolerance = 0.2f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.2f;

        private ProductionControlDrone _drone;
        private bool _enhanced;
        private int _volleyCount;
        private float _startedAt, _firstShotAt, _interval, _direction;
        private float _nextVolleyAt;
        private bool _beganFiring;
        private bool _returnedToIdle;
        private float _recoveryUntil;
        private bool _movementLocked;
        private float _spacingDirection, _spacingDestinationX, _nextSpacingPlanAt;

        public bool DidFire { get; private set; }
        public int FiredVolleys { get; private set; }
        public int ProjectilesFired { get; private set; }
        public float Range => range;
        public override bool ControlsMovement => true;
        protected override bool ShouldShowTelegraph => false;
        private string FiringState => FiredVolleys % 2 == 0 ? "SalvoRearShot" : "SalvoSingleShot";

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
            _startedAt = Time.time;
            _beganFiring = false;
            _returnedToIdle = false;
            _movementLocked = false;
            _spacingDirection = _spacingDestinationX = _nextSpacingPlanAt = 0f;
            _interval = Mathf.Max(0.01f, _enhanced ? lastStandShotInterval : normalShotInterval);
            _firstShotAt = _enhanced ? lastStandFirstShotTime : Mathf.Max(firstShotTime, chargeEndTime);
            _nextVolleyAt = _startedAt + _firstShotAt;
            _drone.StopMovement();
            Enemy.FaceTarget();
            _direction = Enemy.FacingDirection;
            visual.Play("Charge", _enhanced ? 1.2f : 1f);
        }

        protected override void OnReleaseAttack() => CoolTimeStartTime = Time.time;

        private void BeginFiring()
        {
            _beganFiring = true;

            UpdateSpacing();
        }

        private void UpdateSpacing()
        {
            if (_movementLocked || CastTarget == null) return;

            float gap = (CastTarget.position.x - Enemy.transform.position.x) * _direction;

            if (gap <= 0f)
            {
                _movementLocked = true;
                _drone.StopMovement();

                return;
            }

            if (!_beganFiring) return;

            float difference = gap - preferredDistance;

            if (Mathf.Abs(difference) <= distanceTolerance)
            {
                float plannedGap = (CastTarget.position.x - _spacingDestinationX) * _direction;

                // 유효한 목적지로 가는 중이면 기존 감속을 끝까지 재생합니다.
                if (_drone.IsMoving && Mathf.Abs(plannedGap - preferredDistance) > distanceTolerance)
                    _drone.StopMovement();

                return;
            }

            float direction = _direction * Mathf.Sign(difference);

            if (_drone.IsMoving && direction == _spacingDirection) return;
            if (_drone.IsMoving) _drone.StopMovement();
            if (Time.time < _nextSpacingPlanAt) return;

            _nextSpacingPlanAt = Time.time + 0.1f;

            if (_drone.TryPlanGroundMove(direction, Mathf.Abs(difference), out Vector2 destination))
            {
                _spacingDirection = direction;
                _spacingDestinationX = destination.x;
                _drone.MoveGround(destination, Enemy.MoveSpeed);
            }
        }

        private void FireVolley()
        {
            bool rear = FiredVolleys % 2 != 0;
            Transform launcher = rear && rearMuzzle != null ? rearMuzzle : muzzle;
            float offset = _enhanced ? 0f : Random.Range(-verticalSpawnOffset, verticalSpawnOffset);
            // 강화는 같은 기준점에서 쏴 앞뒤 총구 간격으로 탄이 쌍으로 뭉쳐 보이지 않게 합니다.
            // 발사 모션은 기존처럼 앞뒤 런처를 번갈아 재생합니다.
            Vector3 origin = (_enhanced ? muzzle : launcher).position + Vector3.up * offset;
            int count = _enhanced ? 3 : 1;

            for (int i = 0; i < count; i++)
            {
                float angle = i * lastStandSpreadAngle * Mathf.Deg2Rad;
                Vector2 shotDirection = new Vector2(_direction * Mathf.Cos(angle), Mathf.Sin(angle));
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
            visual.Play(FiringState, _enhanced ? lastStandFiringAnimationSpeed : firingAnimationSpeed);
            NotifyAttackPerformed(origin);
        }

        public override void FixedUpdateAttack()
        {
            if (!IsUsing || !HasReleased || IsRecovering) return;

            UpdateSpacing();
            _drone.TickGroundMovement();
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

            UpdateSpacing();

            float elapsed = Time.time - _startedAt;
            if (elapsed < _firstShotAt) return;
            if (!_beganFiring) BeginFiring();

            // 프레임이 늦어져도 밀린 여러 회차를 한꺼번에 생성하지 않습니다.
            if (FiredVolleys < _volleyCount && Time.time >= _nextVolleyAt)
            {
                FireVolley();
                _nextVolleyAt = Time.time + _interval;
            }

            if (FiredVolleys < _volleyCount) return;

            if (!_returnedToIdle)
            {
                if (!visual.HasFinished(FiringState)) return;

                _returnedToIdle = true;
                _drone.StopMovement();
                visual.Play("Idle");
                BeginRecovery();
                _recoveryUntil = Time.time + recoveryTime;
            }
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
