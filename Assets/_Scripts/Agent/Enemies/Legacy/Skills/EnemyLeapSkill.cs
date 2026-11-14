using System.Collections.Generic;
using _Scripts.Agent.SkillSystem.NormalSkill;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Skills
{
    // 준비 시작에 착지점을 고정하고, 실제 착지한 위치에서 피해를 판정합니다.
    public sealed class EnemyLeapSkill : EnemyAttackSkill
    {
        [Header("Condition / Terrain")]
        [SerializeField] private AbstractCoolTimeSkill requiredCoolingSkill;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private LayerMask terrainLayers = (1 << 6) | (1 << 10);
        [SerializeField, Min(0f)] private float maximumLandingHeightDifference = 0.5f;

        [Header("Leap Motion")]
        [SerializeField, Min(0.1f)] private float leapHeight = 1.4f;
        [SerializeField, Min(0.1f)] private float riseDuration = 0.46f;
        [SerializeField, Min(0.1f)] private float diveDuration = 0.2f;
        [Tooltip("값이 클수록 가로 이동을 천천히 시작하고 착지 전 빠르게 움직입니다.")]
        [SerializeField, Range(0f, 1f)] private float horizontalAcceleration = 0.8f;

        [Header("Landing Attack")]
        [SerializeField, Min(0f)] private float damage = 18f;
        [SerializeField, Min(0.01f)] private float landingRadius = 1f;
        [SerializeField, Min(0.01f)] private float activeDuration = 0.15f;
        [Tooltip("착지 판정이 끝난 뒤 다음 공격을 기다리는 시간입니다.")]
        [SerializeField, Min(0f)] private float recoveryTime = 1f;
        [Tooltip("실제 바닥에 닿는 순간 EnemyParticleModule이 재생할 파티클 프리팹입니다.")]
        [SerializeField] private ParticleSystem landingParticlePrefab;

        [Header("Landing Warning")]
        [Tooltip("발사 직전 이 시간 동안 착지 피해 범위를 표시합니다.")]
        [SerializeField, Min(0.05f)] private float warningLeadTime = 0.35f;

        [Header("Prototype Pose")]
        [Tooltip("콜라이더가 없는 그림 전용 부모. 피격 스케일 대상과 별도로 둡니다.")]
        [SerializeField] private Transform poseRoot;
        [SerializeField] private Animator attackAnimator;
        [SerializeField] private string preparationStateName = "Base Layer.Leap";
        [SerializeField] private string flightStateName = "Base Layer.Leap";
        [SerializeField] private string landingStateName = "Base Layer.LeapLand";
        [SerializeField] private string idleStateName = "Base Layer.Idle";
        [SerializeField, Range(0.2f, 1f)] private float crouchScaleY = 0.72f;
        [SerializeField, Min(0f)] private float preparationShake = 0.015f;

        private readonly List<RaycastHit2D> _terrainHits = new List<RaycastHit2D>();
        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private readonly List<ContactPoint2D> _landingContacts = new List<ContactPoint2D>();
        private Rigidbody2D _body;
        private EnemyMovementModule _movement;
        private EnemySkillModule _skills;
        private EnemyParticleModule _particles;
        private ContactFilter2D _terrainFilter;
        private Vector3 _posePosition;
        private Vector3 _poseScale;
        private bool _airborne;
        private bool _landed;
        private bool _hitApplied;
        private float _launchTime;
        private float _flightDeadline;
        private float _activeUntil;
        private float _recoveryUntil;
        private float _direction;
        private Vector2 _lockedLandingCenter;
        private Vector2 _impactCenter;
        private float _warningStartsAt;
        private LeapTrajectory _trajectory;
        private int _flightStep;
        private float _originalGravityScale;
        private bool _followingTrajectory;

        private readonly struct LeapTrajectory
        {
            private readonly Vector2 _start;
            private readonly Vector2 _end;
            private readonly float _peakY;
            private readonly int _riseSteps;
            private readonly float _horizontalAcceleration;

            public int Steps { get; }

            public LeapTrajectory(Vector2 start, Vector2 end, float peakY, int riseSteps, int diveSteps,
                float horizontalAcceleration)
            {
                _start = start;
                _end = end;
                _peakY = peakY;
                _riseSteps = riseSteps;
                Steps = riseSteps + diveSteps;
                _horizontalAcceleration = horizontalAcceleration;
            }

            public Vector2 PositionAt(int step)
            {
                float progress = step / (float)Steps;
                float x = Mathf.Lerp(_start.x, _end.x,
                    Mathf.Lerp(progress, progress * progress, _horizontalAcceleration));
                if (step <= _riseSteps)
                {
                    float remaining = 1f - step / (float)_riseSteps;
                    return new Vector2(x, Mathf.Lerp(_peakY, _start.y, remaining * remaining));
                }

                float dive = (step - _riseSteps) / (float)(Steps - _riseSteps);
                return new Vector2(x, Mathf.Lerp(_peakY, _end.y, dive * dive * dive));
            }
        }

        public override bool ControlsMovement => IsUsing && _airborne;
        public override bool CompletesAfterTargetLost => HasReleased;
        internal override bool FinishesOutsideChaseRange => IsUsing;
        protected override bool ShouldShowTelegraph => IsPreparing && Time.time >= _warningStartsAt;

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            _body = owner.GetComponent<Rigidbody2D>();
            _movement = owner.GetModule<EnemyMovementModule>();
            _skills = owner.GetModule<EnemySkillModule>();
            _particles = owner.GetModule<EnemyParticleModule>();
            _terrainFilter = new ContactFilter2D { useTriggers = false };
            _terrainFilter.SetLayerMask(terrainLayers);
            if (poseRoot != null)
            {
                _posePosition = poseRoot.localPosition;
                _poseScale = poseRoot.localScale;
            }
        }

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() &&
            requiredCoolingSkill != null && requiredCoolingSkill.RemainingCoolTime > 0f &&
            TryChooseLandingCenter(Enemy.Target.position, out _);

        protected override void PrepareAttack()
        {
            _airborne = _landed = _hitApplied = false;
            if (CastTarget == null || !TryChooseLandingCenter(CastTarget.position, out _lockedLandingCenter))
            {
                StopSkill();
                return;
            }
            Enemy.FaceTarget();
            _warningStartsAt = Time.time + Mathf.Max(0f, AttackDelay - warningLeadTime);
            PlayAnimation(preparationStateName);
        }

        protected override void UpdateTelegraph() =>
            Telegraph.SetBounds(new Bounds(_lockedLandingCenter, Vector3.one * (2f * landingRadius)));

        public override void UpdateSkill()
        {
            if (IsPreparing && CanContinue && poseRoot != null)
            {
                poseRoot.localScale = Vector3.Scale(_poseScale, new Vector3(1.08f, crouchScaleY, 1f));
                poseRoot.localPosition = _posePosition + Vector3.right *
                    (Mathf.Sin(Time.time * 90f) * preparationShake);
            }
            base.UpdateSkill();
            if (!IsUsing || !HasReleased) return;
            // 피격 경직으로 FixedUpdate의 이동 처리가 잠겨도 착지는 확인합니다.
            if (_airborne)
            {
                if (Enemy.IsHitReacting) StopFollowingTrajectory();
                CheckLanding();
            }
            if (!IsUsing || !_landed) return;
            if (Time.time < _activeUntil) TryLandingHit();
            else if (!IsRecovering)
            {
                BeginRecovery();
                PlayAnimation(idleStateName);
            }
            if (poseRoot != null)
            {
                float progress = Mathf.Clamp01((Time.time - (_activeUntil - activeDuration)) / 0.2f);
                poseRoot.localScale = Vector3.Scale(_poseScale,
                    Vector3.Lerp(new Vector3(1.15f, 0.8f, 1f), Vector3.one, progress));
            }
            if (Time.time >= _recoveryUntil) EndSkill();
        }

        protected override void OnReleaseAttack()
        {
            RestorePose();
            // 준비 때 정한 착지점의 바닥과 경로가 여전히 유효한지 검사합니다.
            if (!TryPlanLeap(_lockedLandingCenter.x, out _trajectory, out Vector2 landingCenter) ||
                Mathf.Abs(landingCenter.y - _lockedLandingCenter.y) > 0.01f)
            {
                StopSkill();
                return;
            }
            Enemy.StopHorizontalMovement();
            _direction = Enemy.FacingDirection;
            _originalGravityScale = _body.gravityScale;
            _body.gravityScale = 0f;
            _followingTrajectory = true;
            _flightStep = 0;
            _airborne = true;
            _launchTime = Time.time;
            _flightDeadline = Time.time + _trajectory.Steps * Time.fixedDeltaTime + 1f;
            PlayAnimation(flightStateName);
            Telegraph?.Hide();
        }

        public override void FixedUpdateAttack()
        {
            CheckLanding();
            if (!_airborne || !_followingTrajectory) return;
            if (_flightStep >= _trajectory.Steps)
            {
                // 바닥 접촉이 물리 스텝 경계에 늦어지는 경우, 목표 X에서 중력으로 마저 떨어집니다.
                _body.linearVelocityX = 0f;
                StopFollowingTrajectory();
                return;
            }

            Vector2 from = _trajectory.PositionAt(_flightStep);
            Vector2 to = _trajectory.PositionAt(++_flightStep);
            _body.linearVelocity = (to - from) / Time.fixedDeltaTime;
        }

        private void StopFollowingTrajectory()
        {
            if (!_followingTrajectory) return;
            _followingTrajectory = false;
            if (_body != null) _body.gravityScale = _originalGravityScale;
        }

        private void CheckLanding()
        {
            if (!_airborne || _body == null || _movement == null) return;
            // 이동용 GroundCheck는 바닥 직전에도 참입니다. 착지 피해는 실제 접촉 후에만 냅니다.
            if (Time.time > _launchTime + 0.08f && _body.linearVelocityY <= 0.1f &&
                _movement.IsGrounded && bodyCollider.IsTouching(_terrainFilter))
            {
                _airborne = false;
                _landed = true;
                StopFollowingTrajectory();
                _body.linearVelocityX = 0f;
                Bounds bounds = bodyCollider.bounds;
                _impactCenter = new Vector2(bounds.center.x, bounds.min.y + 0.05f);
                _activeUntil = Time.time + Mathf.Max(0.01f, activeDuration);
                _recoveryUntil = _activeUntil + Mathf.Max(0f, recoveryTime);
                PlayAnimation(landingStateName);
                PlayLandingParticle(bounds);
                TryLandingHit();
            }
            else if (Time.time >= _flightDeadline)
            {
                // 지형이 런타임에 제거된 경우 공중에서 가짜 착지 피해를 주지 않습니다.
                StopSkill();
            }
        }

        private void PlayLandingParticle(Bounds bounds)
        {
            if (landingParticlePrefab == null || _particles == null) return;

            // 바닥 접점이 두 개라면 가운데에 재생합니다. 발바닥 중앙을 예비 위치로 둡니다.
            bodyCollider.GetContacts(_terrainFilter, _landingContacts);
            Vector2 contactSum = Vector2.zero;
            int contactCount = 0;
            foreach (ContactPoint2D contact in _landingContacts)
            {
                if (contact.normal.y <= 0.5f) continue;
                contactSum += contact.point;
                contactCount++;
            }
            Vector2 point = contactCount > 0
                ? contactSum / contactCount
                : new Vector2(bounds.center.x, bounds.min.y);
            _particles.PlayLandingEffect(landingParticlePrefab, new Vector3(point.x, point.y, bounds.center.z));
        }

        private void TryLandingHit()
        {
            if (_hitApplied || !CanContinue) return;
            Physics2D.SyncTransforms();
            Physics2D.OverlapCircle(_impactCenter, landingRadius,
                new ContactFilter2D { useTriggers = true }, _overlaps);
            foreach (Collider2D hit in _overlaps)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit, CastTarget)) continue;
                if (!EnemyDamageUtility.TryApply(hit, CastTarget, Enemy, damage,
                        hit.ClosestPoint(_impactCenter), Vector2.right * _direction)) continue;
                _hitApplied = true;
                break;
            }
        }

        private bool TryChooseLandingCenter(Vector2 targetPosition, out Vector2 landingCenter)
        {
            landingCenter = default;
            return _skills != null && _skills.IsTargetInSkillRange(this, targetPosition) &&
                TryPlanLeap(targetPosition.x, out _, out landingCenter);
        }

        private bool TryPlanLeap(float landingX, out LeapTrajectory trajectory, out Vector2 landingCenter)
        {
            trajectory = default;
            landingCenter = default;
            if (_body == null || !_body.simulated || bodyCollider == null || !bodyCollider.enabled ||
                _movement == null || !_movement.IsGrounded) return false;
            Bounds bounds = bodyCollider.bounds;
            if (!TryFindFloor(landingX, bounds.min.y, out float floorY)) return false;
            // 몸체 폭 양쪽이 같은 높이의 바닥 위에 있어야 절벽 끝에 반만 걸치지 않습니다.
            for (int side = -1; side <= 1; side += 2)
                if (!TryFindFloor(landingX + side * bounds.extents.x * 0.8f, bounds.min.y, out float edgeY) ||
                    Mathf.Abs(edgeY - floorY) > 0.1f) return false;
            landingCenter = new Vector2(landingX, floorY + 0.05f);

            float peakY = Mathf.Max(bounds.min.y, floorY) + Mathf.Max(0.1f, leapHeight) + bounds.extents.y;
            int riseSteps = Mathf.CeilToInt(Mathf.Max(0.1f, riseDuration) / Time.fixedDeltaTime);
            int diveSteps = Mathf.CeilToInt(Mathf.Max(0.1f, diveDuration) / Time.fixedDeltaTime);
            trajectory = new LeapTrajectory(bounds.center, new Vector2(landingX, floorY + bounds.extents.y),
                peakY, riseSteps, diveSteps, Mathf.Clamp01(horizontalAcceleration));
            Vector2 previous = bounds.center;
            Vector2 probeSize = Vector2.Max((Vector2)bounds.size - Vector2.one * 0.06f, Vector2.one * 0.04f);
            // 경로 검사와 비행에 같은 스텝 좌표를 사용합니다.
            for (int i = 1; i <= trajectory.Steps; i++)
            {
                Vector2 next = trajectory.PositionAt(i);
                Vector2 delta = next - previous;
                Physics2D.BoxCast(previous, probeSize, 0f, delta.normalized, _terrainFilter, _terrainHits, delta.magnitude);
                foreach (RaycastHit2D hit in _terrainHits)
                {
                    if (!IsTerrain(hit)) continue;
                    if (i == trajectory.Steps && hit.normal.y > 0.5f && Mathf.Abs(hit.point.y - floorY) < 0.1f) continue;
                    return false;
                }
                previous = next;
            }
            return true;
        }

        private bool TryFindFloor(float x, float feetY, out float floorY)
        {
            floorY = 0f;
            float range = Mathf.Max(0f, maximumLandingHeightDifference) + 0.1f;
            Physics2D.Raycast(new Vector2(x, feetY + range), Vector2.down,
                _terrainFilter, _terrainHits, range * 2f);
            foreach (RaycastHit2D hit in _terrainHits)
            {
                if (!IsTerrain(hit) || hit.normal.y <= 0.5f ||
                    Mathf.Abs(hit.point.y - feetY) > maximumLandingHeightDifference + 0.01f) continue;
                floorY = hit.point.y;
                return true;
            }
            return false;
        }

        private bool IsTerrain(RaycastHit2D hit) => hit.collider != null && hit.rigidbody != _body &&
            hit.collider.GetComponentInParent<Agent>() == null;

        public override void OnAttackHit() { }
        public override void OnAttackFinished() { }

        protected override void OnEndSkill() { base.OnEndSkill(); ClearLeap(); }
        protected override void OnStopSkill() { base.OnStopSkill(); ClearLeap(); }

        private void ClearLeap()
        {
            StopFollowingTrajectory();
            if (_airborne && _body != null) _body.linearVelocityX = 0f;
            _airborne = _landed = false;
            RestorePose();
            if (Enemy != null && !Enemy.IsDead) PlayAnimation(idleStateName);
        }

        private void RestorePose()
        {
            if (poseRoot == null) return;
            poseRoot.localPosition = _posePosition;
            poseRoot.localScale = _poseScale;
        }

        private void PlayAnimation(string stateName)
        {
            if (attackAnimator == null || !attackAnimator.isActiveAndEnabled ||
                attackAnimator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName)) return;
            int hash = Animator.StringToHash(stateName);
            if (attackAnimator.HasState(0, hash)) attackAnimator.Play(hash, 0, 0f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!IsUsing) return;
            Vector2 center = _landed ? _impactCenter : _lockedLandingCenter;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, landingRadius);
        }
#endif
    }
}
