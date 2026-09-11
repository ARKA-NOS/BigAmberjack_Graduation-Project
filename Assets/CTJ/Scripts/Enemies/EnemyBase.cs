using System.Collections;
using Agents;
using Agents.Players;
using CTJ.Enemies.FSM;
using DevLib.BattleSystem;
using Lrw.Script._Core._FSM;
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
        [SerializeField, Min(0f)] protected float attackRange = 1.5f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 3f;
        [Tooltip("오른쪽을 기본 방향으로 만든 비주얼 자식. 공격 판정과 발사 위치도 이 아래에 둡니다.")]
        [SerializeField] private Transform facingRoot;

        [Header("Attack")]
        [SerializeField, Min(0.01f)] private float attackInterval = 1f;

        [Header("Death")]
        [Tooltip("선택 사항. 사망 시 Base Layer.Death 상태를 재생합니다.")]
        [SerializeField] private Animator deathAnimator;
        [Tooltip("사망 후 제거까지의 시간. 사망 클립 길이보다 길게 둡니다.")]
        [SerializeField, Min(0f)] private float deathDespawnDelay = 1.1f;

        private Rigidbody2D _rigidbody;
        private StateMachine<EnemyStateType> _stateMachine;
        private float _nextAttackTime;

        public Transform Target => target;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float AttackInterval => attackInterval;
        public virtual bool IsAttackInProgress => false;
        public bool IsDead { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = GetComponent<Rigidbody2D>();
            InitializeStateMachine();
            if (HealthModule != null)
            {
                HealthModule.OnHealthChanged += HandleHealthChanged;
                if (HealthModule.CurrentHealth <= 0f)
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
            if (!IsDead)
                _stateMachine.Update();
        }

        private void FixedUpdate()
        {
            if (!IsDead)
                _stateMachine.FixedUpdate();
        }

        private void InitializeStateMachine()
        {
            _stateMachine = new StateMachine<EnemyStateType>();
            _stateMachine.AddState(EnemyStateType.Idle, new EnemyIdleState(this));
            _stateMachine.AddState(EnemyStateType.Chase, new EnemyChaseState(this));
            _stateMachine.AddState(EnemyStateType.Attack, new EnemyAttackState(this));
            _stateMachine.ChangeState(EnemyStateType.Idle);
        }

        public void SetTarget(Transform newTarget)
        {
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
                    target = player.transform;
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
            if (IsDead || target == null)
                return false;

            Vector2 currentPosition = transform.position;
            Vector2 targetPosition = target.position;
            return (targetPosition - currentPosition).sqrMagnitude <= range * range;
        }

        internal void MoveTowardsTarget()
        {
            if (IsDead || target == null)
            {
                StopHorizontalMovement();
                return;
            }

            float horizontalDifference = target.position.x - transform.position.x;
            float direction = Mathf.Sign(horizontalDifference);

            if (Mathf.Approximately(horizontalDifference, 0f))
                direction = 0f;

            _rigidbody.linearVelocityX = direction * moveSpeed;
            OnMoveDirectionChanged(direction);
        }

        internal void StopHorizontalMovement()
        {
            _rigidbody.linearVelocityX = 0f;
        }

        internal void ExecuteAttack()
        {
            if (IsDead || IsAttackInProgress || Time.time < _nextAttackTime)
                return;

            // 상태를 나갔다 들어와도 공격 간격이 초기화되지 않습니다.
            _nextAttackTime = Time.time + AttackInterval;
            if (target != null)
                OnMoveDirectionChanged(target.position.x - transform.position.x);
            Attack();
        }

        protected virtual void OnMoveDirectionChanged(float direction)
        {
            if (facingRoot == null || facingRoot == transform || Mathf.Approximately(direction, 0f))
                return;

            Vector3 scale = facingRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
            facingRoot.localScale = scale;
        }

        protected abstract void Attack();

        // 공용 Agent.ApplyDamage는 virtual이 아니므로 숨기고, CTJ의 IDamageable 구현으로 이 메서드를 사용합니다.
        public new virtual void ApplyDamage(
            DamageData damageData,
            Vector2 hitPoint,
            Vector2 hitDirection,
            Vector2 hitNormal)
        {
            float amount = damageData.DamageAmount;
            if (IsDead || !isActiveAndEnabled || HealthModule == null ||
                amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
                return;

            HealthModule.CurrentHealth -= amount;
            OnHit?.Invoke();
        }

        private void HandleHealthChanged(float current, float delta, float max)
        {
            if (current <= 0f)
                Die();
        }

        private void Die()
        {
            if (IsDead)
                return;

            // 외부 콜백이나 남은 Animation Event보다 먼저 사망 상태를 확정합니다.
            IsDead = true;
            StopAllCoroutines();
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.simulated = false;
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
                collider.enabled = false;

            // MeleeEnemy.OnDisable이 진행 중인 공격과 타격 대상도 정리합니다.
            enabled = false;
            if (deathAnimator != null && deathAnimator.isActiveAndEnabled &&
                deathAnimator.runtimeAnimatorController != null && deathAnimator.HasState(0, DeathStateHash))
            {
                deathAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                deathAnimator.Play(DeathStateHash, 0, 0f);
            }
            Destroy(gameObject, Mathf.Max(0f, deathDespawnDelay));
        }

        private void OnDestroy()
        {
            if (HealthModule != null)
                HealthModule.OnHealthChanged -= HandleHealthChanged;
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
            attackRange = Mathf.Clamp(attackRange, 0f, detectionRange);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            attackInterval = Mathf.Max(0.01f, attackInterval);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
