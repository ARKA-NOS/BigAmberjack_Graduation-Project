using System;
using System.Collections;
using _Scripts._Core._FSM;
using _Scripts.Agent.Enemies.FSM;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.Players;
using _Scripts.DevLib.BattleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBase : Agent, IDamageable
    {
        private const float TargetSearchInterval = 0.2f;
        private static readonly int DeathStateHash = Animator.StringToHash("Base Layer.Death");

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] protected float detectionRange = 6f;
        [Tooltip("한 번 추적한 뒤 이 거리 밖으로 나가야 해제합니다. 배회 모듈이 있으면 배회를 재개하고, 없으면 현재 위치에서 대기합니다.")]
        [SerializeField, Min(0f)] private float chaseReleaseRange = 12f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 3f;
        [Tooltip("오른쪽을 기본 방향으로 만든 비주얼 자식. 공격 판정과 발사 위치도 이 아래에 둡니다.")]
        [SerializeField] private Transform facingRoot;

        [Header("Death")]
        [Tooltip("선택 사항. 사망 시 Base Layer.Death 상태를 재생합니다.")]
        [SerializeField] private Animator deathAnimator;
        [Tooltip("Death 애니메이션이 없는 적이 사망 후 페이드를 시작하기까지의 시간입니다.")]
        [SerializeField, Min(0f)] private float deathDespawnDelay = 1.1f;
        [Tooltip("사망 애니메이션 마지막 프레임을 유지하며 사라지는 시간입니다.")]
        [SerializeField, Min(0.01f)] private float deathFadeDuration = 0.2f;

        private Rigidbody2D _rigidbody;
        private EnemyMovementModule _movement;
        private EnemyPatrolModule _patrol;
        private EnemyTurnDelayModule _turnDelay;
        private EnemyKeepDistanceModule _keepDistance;
        private EnemySkillModule _skills;
        private EnemyHitReactionModule _hitReaction;
        private EnemyCurrencyRewardModule _currencyReward;
        private EnemyDeathModule _death;
        private StateMachine<EnemyStateType> _stateMachine;
        private (DamageData damage, Vector2 point, Vector2 direction, Vector2 normal, bool suppressHitControl)? _pendingHit;
        private bool _deathFinalized;
        private bool _hasDeathAnimation;
        private float _deathStartedAt;

        public Transform Target => target;
        public float DetectionRange => detectionRange;
        public float ChaseReleaseRange => Mathf.Max(detectionRange, chaseReleaseRange);
        public float AttackRange => _skills != null ? _skills.MaximumRange : 0f;
        public virtual bool IsAttackInProgress => _skills != null && _skills.IsBusy;
        internal bool HasCommittedAttack => _skills != null && _skills.HasCommittedAttack;
        internal bool FinishesAttackOutsideChaseRange =>
            _skills?.ActiveSkill is EnemyAttackSkill { FinishesOutsideChaseRange: true };
        public bool IsDead { get; private set; }
        public bool IsPlayingDeathReaction { get; private set; }
        // 실제 HP가 줄어든 타격만 전달합니다. DamageAmount는 방어/잔여 HP를 반영한 감소량입니다.
        // 사망 타격도 포함하며 크리티컬 여부와 피격 위치/방향을 보존합니다.
        public event Action<DamageData, Vector2, Vector2, Vector2> DamageReceived;
        // HP가 0 이하가 된 프레임에 사망 애니메이션과 함께 한 번 전달합니다.
        public event Action DeathStarted;
        public bool IsHitReacting => _hitReaction != null && _hitReaction.IsReacting;
        internal float FacingDirection => facingRoot != null && facingRoot.localScale.x < 0f ? -1f : 1f;
        internal float MoveSpeed => moveSpeed;
        protected virtual bool UsesGroundMovement => true;

        protected override void Awake()
        {
            // 이동 모듈은 자식에 둘 수 있습니다. 구형 프리팹에 없는 경우에만 수집 전에 보완합니다.
            if (UsesGroundMovement && GetComponentInChildren<EnemyMovementModule>() == null)
                gameObject.AddComponent<EnemyMovementModule>();
            base.Awake();

            _rigidbody = GetComponent<Rigidbody2D>();
            _movement = GetModule<EnemyMovementModule>();
            _patrol = GetModule<EnemyPatrolModule>();
            _turnDelay = GetModule<EnemyTurnDelayModule>();
            _keepDistance = GetModule<EnemyKeepDistanceModule>();
            _skills = GetModule<EnemySkillModule>();
            _hitReaction = GetModule<EnemyHitReactionModule>();
            _currencyReward = GetModule<EnemyCurrencyRewardModule>();
            _death = GetModule<EnemyDeathModule>();
            InitializeAI();
            if (NewHealthModule != null)
            {
                NewHealthModule.OnHealthChanged += HandleHealthChanged;
                if (NewHealthModule.CurrentHealth <= 0f)
                    Die();
            }
        }

        protected override void Start()
        {
            base.Start();

            if (!IsDead && target == null)
                StartCoroutine(FindTargetRoutine());
        }

        private void Update()
        {
            if (_turnDelay != null && (IsDead || IsHitReacting ||
                _skills?.ActiveSkill is EnemyAttackSkill { IsPreparing: false }))
                _turnDelay.ResetTurn();
            if (!IsDead && !IsHitReacting)
                UpdateAI();
        }

        private void FixedUpdate()
        {
            if (IsDead)
            {
                if (IsPlayingDeathReaction && _hitReaction != null) _hitReaction.TickReaction();
                return;
            }
            if (_hitReaction != null && _hitReaction.TickReaction()) return;
            if (_skills != null && _skills.ActiveSkill is EnemyAttackSkill { ControlsMovement: true } attack)
            {
                attack.FixedUpdateAttack();
                return;
            }
            FixedUpdateAI();
        }

        protected virtual void UpdateAI() => _stateMachine.Update();
        protected virtual void FixedUpdateAI() => _stateMachine.FixedUpdate();

        protected virtual void InitializeAI()
        {
            _stateMachine = new StateMachine<EnemyStateType>();
            _stateMachine.AddState(EnemyStateType.Idle, new EnemyIdleState(this));
            _stateMachine.AddState(EnemyStateType.Chase, new EnemyChaseState(this));
            _stateMachine.AddState(EnemyStateType.Attack, new EnemyAttackState(this));
            _stateMachine.ChangeState(EnemyStateType.Idle);
        }

        public virtual void SetTarget(Transform newTarget)
        {
            if (target != newTarget)
            {
                _turnDelay?.ResetTurn();
                if (_skills != null && !HasCommittedAttack) _skills.StopAttack();
            }
            target = newTarget;
        }

        private IEnumerator FindTargetRoutine()
        {
            WaitForSeconds searchDelay = new WaitForSeconds(TargetSearchInterval);

            while (target == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    SetTarget(player.transform);
                    yield break;
                }

                yield return searchDelay;
            }
        }

        internal void ChangeState(EnemyStateType stateType)
        {
            if (!IsDead)
                _stateMachine.ChangeState(stateType);
        }

        internal bool IsTargetInRange(float range)
        {
            if (IsDead || target == null || !target.gameObject.activeInHierarchy)
                return false;

            Vector2 currentPosition = transform.position;
            Vector2 targetPosition = target.position;
            return (targetPosition - currentPosition).sqrMagnitude <= range * range;
        }

        internal void MoveTowardsTarget()
        {
            if (IsDead || target == null || _movement == null)
            {
                StopHorizontalMovement();
                return;
            }

            bool hasTurnDelay = _turnDelay != null && _turnDelay.isActiveAndEnabled;
            if (hasTurnDelay && !TryFaceTarget())
            {
                StopHorizontalMovement();
                return;
            }

            float direction = _keepDistance != null && _keepDistance.isActiveAndEnabled
                ? _keepDistance.MoveTowardsTarget(target, moveSpeed)
                : _movement.MoveTowards(target, moveSpeed);
            if (!hasTurnDelay) OnMoveDirectionChanged(direction);
        }

        internal void StopHorizontalMovement()
        {
            if (_movement != null)
                _movement.Stop();
            else
                _rigidbody.linearVelocityX = 0f;
        }

        internal void BeginPatrol()
        {
            _turnDelay?.ResetTurn();
            StopHorizontalMovement();
            if (_patrol != null) _patrol.BeginPatrol();
        }

        internal void TickPatrol()
        {
            if (_patrol == null || !_patrol.isActiveAndEnabled)
            {
                StopHorizontalMovement();
                return;
            }
            OnMoveDirectionChanged(_patrol.TickPatrol());
        }

        internal void EndPatrol()
        {
            if (_patrol != null) _patrol.EndPatrol();
            StopHorizontalMovement();
        }

        internal bool CanAttackTarget()
        {
            return !IsHitReacting && _skills != null && _skills.CanAttackTarget();
        }

        internal void ExecuteAttack()
        {
            if (IsDead || IsAttackInProgress || !CanAttackTarget())
                return;

            if (TryFaceTarget()) _skills.TryUseAttack();
        }

        protected virtual void OnMoveDirectionChanged(float direction)
        {
            if (facingRoot == null || facingRoot == transform || Mathf.Approximately(direction, 0f))
                return;

            Vector3 scale = facingRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
            facingRoot.localScale = scale;
        }

        internal void FaceTarget() => TryFaceTarget();

        private bool TryFaceTarget()
        {
            if (target == null) return false;
            float direction = target.position.x - transform.position.x;
            if (_turnDelay != null && _turnDelay.isActiveAndEnabled &&
                !_turnDelay.TryResolveFacing(FacingDirection, direction, out direction)) return false;

            OnMoveDirectionChanged(direction);
            return true;
        }

        public void OnAttackWarning() => _skills?.OnAttackWarning();
        public void OnAttackHit() => _skills?.OnAttackHit();
        public void OnAttackFinished() => _skills?.OnAttackFinished();
        internal void CancelAttack() => _skills?.StopAttack();

        public override void ApplyDamage(
            DamageData damageData,
            Vector2 hitPoint,
            Vector2 hitDirection,
            Vector2 hitNormal)
        {
            float amount = damageData.DamageAmount;
            if (IsDead || !isActiveAndEnabled || NewHealthModule == null ||
                amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
                return;

            amount = ModifyIncomingDamage(damageData, hitPoint, hitDirection, out bool suppressHitControl);
            if (amount <= 0f) return;

            var previousHit = _pendingHit;
            _pendingHit = (damageData, hitPoint, hitDirection, hitNormal, suppressHitControl);
            try { NewHealthModule.ApplyDamage(amount); }
            finally { _pendingHit = previousHit; }
            OnHit?.Invoke();
        }

        protected virtual float ModifyIncomingDamage(DamageData damage, Vector2 hitPoint, Vector2 hitDirection,
            out bool suppressHitControl)
        {
            suppressHitControl = false;
            return damage.DamageAmount;
        }

        private void HandleHealthChanged(float current, float delta, float max)
        {
            var hit = delta < 0f ? _pendingHit : null;
            if (hit.HasValue)
            {
                _pendingHit = null;
                var appliedHit = hit.Value;
                appliedHit.damage.DamageAmount = -delta;
                hit = appliedHit;
            }
            if (current <= 0f)
                Die(hit);
            else if (hit.HasValue)
            {
                DamageReceived?.Invoke(hit.Value.damage, hit.Value.point, hit.Value.direction, hit.Value.normal);
                _hitReaction?.React(hit.Value.damage, hit.Value.direction, hit.Value.suppressHitControl);
            }
            OnHealthChanged(current, delta, max);
        }

        protected virtual void OnHealthChanged(float current, float delta, float max) { }

        private void Die((DamageData damage, Vector2 point, Vector2 direction, Vector2 normal, bool suppressHitControl)? hit = null)
        {
            if (IsDead)
                return;

            // 외부 콜백이나 남은 Animation Event보다 먼저 사망 상태를 확정합니다.
            IsDead = true;
            IsPlayingDeathReaction = true;
            _deathStartedAt = Time.time;
            _skills?.StopAttack();
            StopAllCoroutines();
            StopHorizontalMovement();
            // 지면용 물리 콜라이더는 마지막 넉백이 끝날 때까지 유지합니다.
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
                if (collider.isTrigger) collider.enabled = false;
            if (hit.HasValue)
            {
                // 파티클은 피격 스케일 변형 전의 그림을 기준으로 접촉점을 보정합니다.
                DamageReceived?.Invoke(hit.Value.damage, hit.Value.point, hit.Value.direction, hit.Value.normal);
                _hitReaction?.React(hit.Value.damage, hit.Value.direction, hit.Value.suppressHitControl);
            }
            if (deathAnimator != null && deathAnimator.isActiveAndEnabled &&
                deathAnimator.runtimeAnimatorController != null && deathAnimator.HasState(0, DeathStateHash))
            {
                _hasDeathAnimation = true;
                deathAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                deathAnimator.Play(DeathStateHash, 0, 0f);
                // 타격이 Animator 평가 뒤에 들어온 프레임에도 사망 첫 장면을 즉시 보여줍니다.
                deathAnimator.Update(0f);
            }
            DeathStarted?.Invoke();
            if (isActiveAndEnabled) StartCoroutine(FinishDeathAfterReaction());
            else FinishDeath();
        }

        private IEnumerator FinishDeathAfterReaction()
        {
            while (_hitReaction != null && _hitReaction.RemainingFeedbackTime > 0f)
                yield return null;
            FinishDeath();
        }

        private void FinishDeath()
        {
            if (_deathFinalized) return;
            _deathFinalized = true;
            IsPlayingDeathReaction = false;
            if (_hitReaction != null) _hitReaction.ClearReaction();
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.simulated = false;
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
                collider.enabled = false;

            if (_death != null) _death.ReportDeath();
            // 제거/씬 전환이 아닌 실제 사망에서만 지급합니다. 외부 재화 이벤트보다 사망 처리를 먼저 끝냅니다.
            if (_currencyReward != null) _currencyReward.GrantDeathReward();
            // 사망 중에는 AI가 이미 IsDead로 멈춰 있습니다. 컴포넌트를 켜 둬 페이드 코루틴을 실행합니다.
            if (isActiveAndEnabled) StartCoroutine(FadeAndDespawn());
            else Destroy(gameObject, Mathf.Max(0f, deathDespawnDelay - (Time.time - _deathStartedAt)));
        }

        private IEnumerator FadeAndDespawn()
        {
            if (_hasDeathAnimation && deathAnimator != null && deathAnimator.isActiveAndEnabled)
            {
                AnimatorStateInfo state = deathAnimator.GetCurrentAnimatorStateInfo(0);
                while (state.fullPathHash == DeathStateHash && state.normalizedTime < 1f)
                {
                    yield return null;
                    if (deathAnimator == null || !deathAnimator.isActiveAndEnabled ||
                        deathAnimator.runtimeAnimatorController == null) break;
                    state = deathAnimator.GetCurrentAnimatorStateInfo(0);
                }
                if (deathAnimator != null && deathAnimator.isActiveAndEnabled &&
                    deathAnimator.runtimeAnimatorController != null && deathAnimator.HasState(0, DeathStateHash))
                {
                    // 전환이나 루프 영향 없이 마지막 그림을 확정한 뒤 그대로 고정합니다.
                    deathAnimator.Play(DeathStateHash, 0, 1f);
                    deathAnimator.Update(0f);
                    deathAnimator.speed = 0f;
                }
            }
            else
            {
                float remaining = deathDespawnDelay - (Time.time - _deathStartedAt);
                if (remaining > 0f) yield return new WaitForSeconds(remaining);
            }

            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
            float[] originalAlphas = new float[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) originalAlphas[i] = sprites[i].color.a;

            float duration = Mathf.Max(0.01f, deathFadeDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] == null) continue;
                    Color color = sprites[i].color;
                    color.a = originalAlphas[i] * alpha;
                    sprites[i].color = color;
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (NewHealthModule != null)
                NewHealthModule.OnHealthChanged -= HandleHealthChanged;
        }

        protected virtual void OnDisable()
        {
            // 연출 중 컴포넌트를 꺼도 대기 상태로 남지 않도록 정리합니다.
            if (IsPlayingDeathReaction) FinishDeath();
            if (_hitReaction != null) _hitReaction.ClearReaction();
            _skills?.StopAttack();
            if (_rigidbody != null) StopHorizontalMovement();
        }

#if UNITY_EDITOR
        [ContextMenu("Test/Take 2 Damage (Play Mode)")]
        protected void TestTakeDamage()
        {
            if (!Application.isPlaying)
                return;
            ((IDamageable)this).ApplyDamage(new DamageData { DamageAmount = 2f },
                transform.position, Vector2.zero, Vector2.zero);
        }
#endif

        private void OnValidate()
        {
            detectionRange = Mathf.Max(0f, detectionRange);
            chaseReleaseRange = Mathf.Max(detectionRange, chaseReleaseRange);
            moveSpeed = Mathf.Max(0f, moveSpeed);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, ChaseReleaseRange);

            Gizmos.color = Color.red;
            EnemySkillModule skillModule = _skills != null ? _skills : GetComponentInChildren<EnemySkillModule>();
            Gizmos.DrawWireSphere(transform.position, skillModule != null ? skillModule.MaximumRange : 0f);
        }
    }
}
