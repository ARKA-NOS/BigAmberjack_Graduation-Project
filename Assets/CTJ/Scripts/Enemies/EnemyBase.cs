using System.Collections;
using CTJ.Enemies.FSM;
using DevLib.BattleSystem;
using Lrw.Script._Core._FSM;
using Lrw.Script.Agent;
using Lrw.Script.Agent.Players;
using UnityEngine;

namespace CTJ.Enemies
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
        [Tooltip("사망 후 제거까지의 시간. 사망 클립 길이보다 길게 둡니다.")]
        [SerializeField, Min(0f)] private float deathDespawnDelay = 1.1f;

        private Rigidbody2D _rigidbody;
        private EnemyMovementModule _movement;
        private EnemyPatrolModule _patrol;
        private EnemyKeepDistanceModule _keepDistance;
        private EnemySkillModule _skills;
        private EnemyHitReactionModule _hitReaction;
        private EnemyCurrencyRewardModule _currencyReward;
        private EnemyDeathModule _death;
        private StateMachine<EnemyStateType> _stateMachine;

        public Transform Target => target;
        public float DetectionRange => detectionRange;
        public float ChaseReleaseRange => Mathf.Max(detectionRange, chaseReleaseRange);
        public float AttackRange => _skills != null ? _skills.MaximumRange : 0f;
        public virtual bool IsAttackInProgress => _skills != null && _skills.IsBusy;
        public bool IsDead { get; private set; }
        public bool IsHitReacting => _hitReaction != null && _hitReaction.IsReacting;
        internal float FacingDirection => facingRoot != null && facingRoot.localScale.x < 0f ? -1f : 1f;
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
            if (!IsDead && !IsHitReacting)
                UpdateAI();
        }

        private void FixedUpdate()
        {
            if (IsDead) return;
            if (_hitReaction != null && _hitReaction.TickReaction()) return;
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
            if (target != newTarget && _skills != null)
                _skills.StopAttack();
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

            float direction = _keepDistance != null && _keepDistance.isActiveAndEnabled
                ? _keepDistance.MoveTowardsTarget(target, moveSpeed)
                : _movement.MoveTowards(target, moveSpeed);
            OnMoveDirectionChanged(direction);
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

            if (target != null)
                OnMoveDirectionChanged(target.position.x - transform.position.x);
            _skills.TryUseAttack();
        }

        protected virtual void OnMoveDirectionChanged(float direction)
        {
            if (facingRoot == null || facingRoot == transform || Mathf.Approximately(direction, 0f))
                return;

            Vector3 scale = facingRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
            facingRoot.localScale = scale;
        }

        internal void FaceTarget()
        {
            if (target != null) OnMoveDirectionChanged(target.position.x - transform.position.x);
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

            NewHealthModule.ApplyDamage(amount);
            // HP 0이 되는 피해는 사망 연출을 우선합니다. 깜빡임은 무적 시간을 만들지 않습니다.
            if (!IsDead && _hitReaction != null) _hitReaction.React(damageData, hitDirection);
            OnHit?.Invoke();
        }

        private void HandleHealthChanged(float current, float delta, float max)
        {
            if (current <= 0f)
                Die();
            OnHealthChanged(current, delta, max);
        }

        protected virtual void OnHealthChanged(float current, float delta, float max) { }

        private void Die()
        {
            if (IsDead)
                return;

            // 외부 콜백이나 남은 Animation Event보다 먼저 사망 상태를 확정합니다.
            IsDead = true;
            if (_hitReaction != null) _hitReaction.ClearReaction();
            _skills?.StopAttack();
            StopAllCoroutines();
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.simulated = false;
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
                collider.enabled = false;

            enabled = false;
            if (deathAnimator != null && deathAnimator.isActiveAndEnabled &&
                deathAnimator.runtimeAnimatorController != null && deathAnimator.HasState(0, DeathStateHash))
            {
                deathAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                deathAnimator.Play(DeathStateHash, 0, 0f);
            }
            Destroy(gameObject, Mathf.Max(0f, deathDespawnDelay));
            if (_death != null) _death.ReportDeath();
            // 제거/씬 전환이 아닌 실제 사망에서만 지급합니다. 외부 재화 이벤트보다 사망 처리를 먼저 끝냅니다.
            if (_currencyReward != null) _currencyReward.GrantDeathReward();
        }

        private void OnDestroy()
        {
            if (NewHealthModule != null)
                NewHealthModule.OnHealthChanged -= HandleHealthChanged;
        }

        protected virtual void OnDisable()
        {
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
