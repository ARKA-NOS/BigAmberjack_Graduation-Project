using DevLib.ModuleSystem;
using UnityEngine;

namespace CTJ.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyPatrolModule : Module
    {
        [Tooltip("스폰 위치에서 좌우로 배회할 수 있는 거리. 룸의 중심이나 포탈 위치는 사용하지 않습니다.")]
        [SerializeField, Min(0f)] private float halfWidth = 3f;
        [Tooltip("저장된 스폰 위치에 더할 배회 중심의 X 오프셋.")]
        [SerializeField] private float centerOffsetX;
        [SerializeField, Min(0f)] private float patrolSpeed = 2f;
        [SerializeField, Min(0f)] private float pauseDuration = 0.6f;

        private EnemyBase _enemy;
        private EnemyMovementModule _movement;
        private Vector2 _spawnPosition;
        private float _destinationX;
        private float _waitUntil;
        private bool _patrolling;
        private bool _waiting;
        private float _nextDirection;

        public Vector2 Center => _spawnPosition + Vector2.right * centerOffsetX;
        public float MinX => Center.x - Mathf.Max(0f, halfWidth);
        public float MaxX => Center.x + Mathf.Max(0f, halfWidth);

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _movement = owner.GetModule<EnemyMovementModule>();
            // 포탈의 비활성 홀더에서 꺼내져 처음 초기화되는 위치를 한 번만 저장합니다.
            _spawnPosition = owner.transform.position;
        }

        internal void BeginPatrol()
        {
            if (_enemy == null) return;
            _patrolling = true;
            _waiting = false;
            _nextDirection = Random.value < 0.5f ? -1f : 1f;
            ChooseDestination();
        }

        internal float TickPatrol()
        {
            if (!isActiveAndEnabled || _enemy == null || _enemy.IsDead || _movement == null)
                return 0f;
            if (!_patrolling) BeginPatrol();
            if (halfWidth <= 0.06f || patrolSpeed <= 0f)
            {
                _movement.Stop();
                return 0f;
            }
            if (_waiting)
            {
                _movement.Stop();
                if (Time.time < _waitUntil) return 0f;
                _waiting = false;
                ChooseDestination();
            }
            _destinationX = Mathf.Clamp(_destinationX, MinX, MaxX);
            float direction = _movement.MoveOnGroundTowards(_destinationX, patrolSpeed, out bool blocked);
            if (blocked || Mathf.Abs(_enemy.transform.position.x - _destinationX) <= 0.06f)
            {
                _movement.Stop();
                _waiting = true;
                _waitUntil = Time.time + Mathf.Max(0f, pauseDuration);
                if (blocked) _nextDirection = _destinationX >= _enemy.transform.position.x ? -1f : 1f;
            }
            return direction;
        }

        private void ChooseDestination()
        {
            float x = _enemy.transform.position.x;
            // 경계 바로 밖에서는 경계점이 이동 허용 오차 안에 있어 정지만 반복할 수 있습니다.
            // 구간 안쪽을 목적지로 정해 실제로 복귀한 뒤 다음 배회 지점을 선택합니다.
            if (x < MinX) { _destinationX = Center.x; _nextDirection = 1f; return; }
            if (x > MaxX) { _destinationX = Center.x; _nextDirection = -1f; return; }
            float end = _nextDirection > 0f ? MaxX : MinX;
            _destinationX = Mathf.Lerp(x, end, Random.Range(0.5f, 1f));
            _nextDirection = -_nextDirection;
        }

        internal void EndPatrol() => _patrolling = false;

        private void OnDisable()
        {
            if (_patrolling && _movement != null) _movement.Stop();
            _patrolling = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            EnemyBase owner = GetComponentInParent<EnemyBase>();
            Transform root = owner != null ? owner.transform : transform;
            Vector2 center = Application.isPlaying && _enemy != null ? Center
                : (Vector2)root.position + Vector2.right * centerOffsetX;
            float width = Mathf.Max(0f, halfWidth);
            float y = root.position.y + 0.15f;
            Vector3 left = new Vector3(center.x - width, y, 0f);
            Vector3 right = new Vector3(center.x + width, y, 0f);
            Color previous = Gizmos.color;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawLine(left - Vector3.up * 0.4f, left + Vector3.up * 0.4f);
            Gizmos.DrawLine(right - Vector3.up * 0.4f, right + Vector3.up * 0.4f);
            Gizmos.DrawWireSphere(new Vector3(center.x, y, 0f), 0.1f);
            UnityEditor.Handles.Label(new Vector3(center.x, y + 0.5f, 0f), $"Spawn patrol width: {width * 2f:0.##}");
            Gizmos.color = previous;
        }
#endif
    }
}
