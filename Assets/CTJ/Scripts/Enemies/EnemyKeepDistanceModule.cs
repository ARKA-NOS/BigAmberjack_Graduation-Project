using DevLib.ModuleSystem;
using UnityEngine;

namespace CTJ.Enemies
{
    // 선택형 지상 이동 규칙. 기존 FSM이 이동을 요청할 때만 실행합니다.
    [DisallowMultipleComponent]
    public sealed class EnemyKeepDistanceModule : Module
    {
        [Tooltip("이 거리보다 가까우면 후퇴합니다. SkillModule의 Min Range와 맞춰 주세요.")]
        [SerializeField, Min(0f)] private float minimumDistance = 5f;
        [Tooltip("후퇴 목적지의 여유 거리. 최소 사거리 경계에서 반복 정지하는 것을 방지합니다.")]
        [SerializeField, Min(0.07f)] private float retreatPadding = 0.15f;

        private EnemyBase _enemy;
        private EnemyMovementModule _movement;
        private float _lastTargetSide = 1f;

        public float MinimumDistance => Mathf.Max(0f, minimumDistance);

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _movement = owner.GetModule<EnemyMovementModule>();
        }

        internal float MoveTowardsTarget(Transform target, float speed)
        {
            if (!isActiveAndEnabled || _enemy == null || _movement == null || target == null)
            {
                _movement?.Stop();
                return 0f;
            }

            Vector2 offset = target.position - _enemy.transform.position;
            if (Mathf.Abs(offset.x) > 0.01f) _lastTargetSide = Mathf.Sign(offset.x);
            float distanceSquared = offset.sqrMagnitude;
            float minimum = MinimumDistance;
            float maximum = _enemy.AttackRange;
            if (distanceSquared >= minimum * minimum && distanceSquared <= maximum * maximum)
            {
                // 사거리와 실제 공격 가능 여부(발판에 가린 시야 등)는 별개입니다.
                // 적정 거리에서도 다시 접근하면 최소 거리 경계에서 접근/후퇴를 반복합니다.
                _movement.Stop();
                _enemy.FaceTarget();
                return 0f;
            }
            float destinationX = target.position.x;
            if (distanceSquared < minimum * minimum)
            {
                // 거리 판정은 스킬과 같은 2D 거리. 높이가 달라도 필요한 수평 거리만 확보합니다.
                float radius = MinimumDistance + Mathf.Max(0.07f, retreatPadding);
                float horizontalDistance = Mathf.Sqrt(Mathf.Max(0f, radius * radius - offset.y * offset.y));
                destinationX = target.position.x - _lastTargetSide * horizontalDistance;
            }

            // 후퇴/접근 모두 기존 벽·낭떠러지 검사 사용. 막혔다고 플레이어 쪽으로 돌진하지 않습니다.
            // 바라볼 방향은 EnemyBase가 실제 이동 방향으로 맞춥니다. 공격 진입 시에는 타깃을 봅니다.
            return _movement.MoveOnGroundTowards(destinationX, speed, out _);
        }

        private void OnDrawGizmosSelected()
        {
            EnemyBase enemy = _enemy != null ? _enemy : GetComponentInParent<EnemyBase>();
            if (enemy == null) return;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(enemy.transform.position, MinimumDistance);
        }
    }
}
