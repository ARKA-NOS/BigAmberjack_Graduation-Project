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

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float detectionRange = 6f;
        [SerializeField, Min(0f)] private float attackRange = 1.5f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 3f;

        [Header("Attack")]
        [SerializeField, Min(0.01f)] private float attackInterval = 1f;

        private Rigidbody2D _rigidbody;
        private StateMachine<EnemyStateType> _stateMachine;

        public Transform Target => target;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float AttackInterval => attackInterval;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = GetComponent<Rigidbody2D>();
            InitializeStateMachine();
        }

        protected override void Start()
        {
            base.Start();

            if (target == null)
                StartCoroutine(FindTargetRoutine());
        }

        private void Update()
        {
            _stateMachine.Update();
        }

        private void FixedUpdate()
        {
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
            _stateMachine.ChangeState(stateType);
        }

        internal bool IsTargetInRange(float range)
        {
            if (target == null)
                return false;

            Vector2 currentPosition = transform.position;
            Vector2 targetPosition = target.position;
            return (targetPosition - currentPosition).sqrMagnitude <= range * range;
        }

        internal void MoveTowardsTarget()
        {
            if (target == null)
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
            Attack();
        }

        protected virtual void OnMoveDirectionChanged(float direction)
        {
        }

        protected abstract void Attack();

        public abstract void ApplyDamage(
            DamageData damageData,
            Vector2 hitPoint,
            Vector2 hitDirection,
            Vector2 hitNormal);

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
