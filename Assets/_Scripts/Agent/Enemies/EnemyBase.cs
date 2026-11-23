using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        private const float DeathLandingTimeout = 3f;
        private const float DeathFallGravityScale = 3f;
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
        [Tooltip("선택 사항. 임시 사망 연출의 공통 설정입니다. SO의 Use Temporary Feedback을 끄면 기존 연출로 돌아갑니다.")]
        [SerializeField] private EnemyDeathFeedbackSettingsSO deathFeedbackSettings;

        private Rigidbody2D _rigidbody;
        private EnemyMovementModule _movement;
        private EnemyFlightModule _flight;
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
        private bool _fadeDeathImmediately;
        private float _immediateDeathFadeDuration;
        private ContactFilter2D _deathGroundFilter;
        private List<ContactPoint2D> _deathGroundContacts;

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
        public bool IsWaitingForDeathLanding { get; private set; }

        // 실제 HP가 줄어든 타격만 전달합니다. DamageAmount는 방어/잔여 HP를 반영한 감소량입니다.
        // 사망 타격도 포함하며 크리티컬 여부와 피격 위치/방향을 보존합니다.
        public event Action<DamageData, Vector2, Vector2, Vector2> DamageReceived;
        // 사망 연출 시작에 한 번 전달합니다. 즉시 페이드 모드에서는 공중에서도 바로 전달합니다.
        public event Action DeathStarted;

        internal EnemyDeathFeedbackSettingsSO DeathFeedbackSettings =>
            deathFeedbackSettings != null && deathFeedbackSettings.UseTemporaryFeedback ? deathFeedbackSettings : null;

        public bool IsHitReacting => _hitReaction != null && _hitReaction.IsReacting;
        internal float FacingDirection => facingRoot != null && facingRoot.localScale.x < 0f ? -1f : 1f;
        internal float MoveSpeed => moveSpeed;
        protected virtual bool UsesGroundMovement => true;

        protected override void Awake()
        {
            // 이동 모듈은 자식에 둘 수 있습니다. 구형 프리팹에 없는 경우에만 수집 전에 보완합니다.
            if (UsesGroundMovement && GetComponentInChildren<EnemyFlightModule>() == null &&
                GetComponentInChildren<EnemyMovementModule>() == null)
                gameObject.AddComponent<EnemyMovementModule>();
            base.Awake();

            _rigidbody = GetComponent<Rigidbody2D>();
            _movement = GetModule<EnemyMovementModule>();
            _flight = GetModule<EnemyFlightModule>();
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
                FindTargetAsync(destroyCancellationToken).Forget();
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

        private async UniTaskVoid FindTargetAsync(CancellationToken cancellationToken)
        {
            TimeSpan searchDelay = TimeSpan.FromSeconds(TargetSearchInterval);

            while (!IsDead && gameObject.activeInHierarchy && target == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    SetTarget(player.transform);
                    return;
                }

                await UniTask.Delay(searchDelay, cancellationToken: cancellationToken);
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
            if (_flight != null && _flight.isActiveAndEnabled && target != null)
            {
                OnMoveDirectionChanged(_flight.Chase(target, moveSpeed));
                return;
            }
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
            if (_flight != null)
                _flight.Stop();
            else if (_movement != null)
                _movement.Stop();
            else
                _rigidbody.linearVelocityX = 0f;
        }

        internal void BeginPatrol()
        {
            _turnDelay?.ResetTurn();
            StopHorizontalMovement();
            if (_flight != null) _flight.BeginPatrol();
            else if (_patrol != null) _patrol.BeginPatrol();
        }

        internal void TickPatrol()
        {
            if (_flight != null && _flight.isActiveAndEnabled)
            {
                OnMoveDirectionChanged(_flight.Patrol(moveSpeed));
                return;
            }
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
            EnemyDeathFeedbackSettingsSO feedback = DeathFeedbackSettings;
            _fadeDeathImmediately = feedback != null && feedback.FadeImmediately;
            _immediateDeathFadeDuration = feedback != null ? feedback.ImmediateFadeDuration : deathFadeDuration;

            _skills?.StopAttack();
            StopHorizontalMovement();

            // 지면용 물리 콜라이더는 마지막 넉백이 끝날 때까지 유지합니다.
            bool hasSolidBody = false;
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
            {
                if (collider.isTrigger)
                    collider.enabled = false;
                else if (collider.attachedRigidbody == _rigidbody && collider.isActiveAndEnabled)
                    hasSolidBody = true;
            }

            IsWaitingForDeathLanding = !_fadeDeathImmediately && ShouldWaitForDeathLanding(hasSolidBody);

            if (hit.HasValue)
            {
                // 파티클은 피격 스케일 변형 전의 그림을 기준으로 접촉점을 보정합니다.
                DamageReceived?.Invoke(hit.Value.damage, hit.Value.point, hit.Value.direction, hit.Value.normal);
                _hitReaction?.React(hit.Value.damage, hit.Value.direction, hit.Value.suppressHitControl);
            }

            if (_deathFinalized) return;

            if (!IsWaitingForDeathLanding) BeginDeathPresentation();
            if (_fadeDeathImmediately)
            {
                // 마지막 피격·사망 이벤트는 그대로 전달하고 연출 종료 대기만 생략합니다.
                FinishDeath();
                return;
            }

            if (isActiveAndEnabled) FinishDeathAfterReactionAsync(destroyCancellationToken).Forget();
            else FinishDeath();
        }

        private bool ShouldWaitForDeathLanding(bool hasSolidBody)
        {
            if (!isActiveAndEnabled || !hasSolidBody || !_rigidbody.simulated ||
                _rigidbody.bodyType != RigidbodyType2D.Dynamic ||
                (_rigidbody.constraints & RigidbodyConstraints2D.FreezePositionY) != 0)
                return false;

            LayerMask terrainLayers = _flight != null ? _flight.TerrainLayers :
                _movement != null ? _movement.TerrainLayers : LayerMask.GetMask("Ground", "Platform");
            _deathGroundFilter = new ContactFilter2D { useTriggers = false };
            _deathGroundFilter.SetLayerMask(terrainLayers);
            _deathGroundContacts ??= new List<ContactPoint2D>(4);
            if (HasDeathGroundContact()) return false;

            if (_rigidbody.gravityScale <= 0f)
                _rigidbody.gravityScale = DeathFallGravityScale;

            return true;
        }

        private bool HasDeathGroundContact()
        {
            if (_rigidbody.linearVelocityY > 0.1f) return false;

            _rigidbody.GetContacts(_deathGroundFilter, _deathGroundContacts);
            foreach (ContactPoint2D contact in _deathGroundContacts)
                if (contact.enabled && contact.normal.y >= 0.5f) return true;

            return false;
        }

        private void BeginDeathPresentation()
        {
            IsWaitingForDeathLanding = false;
            _deathStartedAt = Time.time;

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
        }

        private async UniTaskVoid FinishDeathAfterReactionAsync(CancellationToken cancellationToken)
        {
            if (IsWaitingForDeathLanding)
            {
                // 바닥이 없는 낭떠러지에서도 적과 처치 처리가 남지 않도록 대기를 끝냅니다.
                float deadline = Time.time + DeathLandingTimeout;
                while (!_deathFinalized && Time.time < deadline && !HasDeathGroundContact())
                    await UniTask.NextFrame(cancellationToken);

                if (_deathFinalized) return;
                BeginDeathPresentation();
            }

            while (!_deathFinalized && _hitReaction != null && _hitReaction.RemainingFeedbackTime > 0f)
                await UniTask.NextFrame(cancellationToken);

            FinishDeath();
        }

        private void FinishDeath()
        {
            if (_deathFinalized) return;

            _deathFinalized = true;
            IsWaitingForDeathLanding = false;
            IsPlayingDeathReaction = false;
            if (_hitReaction != null) _hitReaction.ClearReaction();
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.simulated = false;
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
                collider.enabled = false;

            if (_death != null) _death.ReportDeath();
            // 제거/씬 전환이 아닌 실제 사망에서만 지급합니다. 외부 재화 이벤트보다 사망 처리를 먼저 끝냅니다.
            if (_currencyReward != null) _currencyReward.GrantDeathReward();
            // 사망 중에는 AI가 IsDead로 멈추며, 비주얼은 페이드가 끝날 때까지 유지합니다.
            if (isActiveAndEnabled) FadeAndDespawnAsync(destroyCancellationToken).Forget();
            else Destroy(gameObject, _fadeDeathImmediately ? 0f : Mathf.Max(0f, deathDespawnDelay - (Time.time - _deathStartedAt)));
        }

        private async UniTaskVoid FadeAndDespawnAsync(CancellationToken cancellationToken)
        {
            if (!_fadeDeathImmediately && _hasDeathAnimation && deathAnimator != null && deathAnimator.isActiveAndEnabled)
            {
                AnimatorStateInfo state = deathAnimator.GetCurrentAnimatorStateInfo(0);

                while (state.fullPathHash == DeathStateHash && state.normalizedTime < 1f)
                {
                    await UniTask.NextFrame(cancellationToken);
                    if (!gameObject.activeInHierarchy) return;
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
            else if (!_fadeDeathImmediately)
            {
                float remaining = deathDespawnDelay - (Time.time - _deathStartedAt);
                if (remaining > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(remaining), cancellationToken: cancellationToken);
                    if (!gameObject.activeInHierarchy) return;
                }
            }

            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
            float[] originalAlphas = new float[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) originalAlphas[i] = sprites[i].color.a;

            float duration = _fadeDeathImmediately ? _immediateDeathFadeDuration : Mathf.Max(0.01f, deathFadeDuration);
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

                await UniTask.NextFrame(cancellationToken);
                if (!gameObject.activeInHierarchy) return;
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
