using DevLib.ModuleSystem;
using UnityEngine;

namespace CTJ.Enemies.Bosses
{
    // 전투 공간의 설정과 월드 좌표 계산만 담당합니다. 타깃 판별/패턴 취소/투사체 수명은 다루지 않습니다.
    [DisallowMultipleComponent]
    public sealed class BossArenaModule : Module
    {
        [Tooltip("비워두면 생성 위치를 중앙으로 기억합니다. 움직이는 보스 자체가 아닌 고정 앵커를 지정하세요.")]
        [SerializeField] private Transform centerAnchor;
        [SerializeField, Min(1f)] private float halfWidth = 8f;
        [SerializeField, Min(0.1f)] private float floorDistance = 4f;
        [SerializeField, Min(0f)] private float laserHeight = 2.5f;
        [SerializeField, Min(0f)] private float introHeight = 4f;
        [Tooltip("레이저 횡단마다 오른쪽/왼쪽에 번갈아 남길 안전 구역의 가로 비율입니다.")]
        [SerializeField, Range(0.05f, 0.45f)] private float laserSafeWidthRatio = 0.2f;

        public Vector2 Center { get; private set; }
        public float FloorY => Center.y - Mathf.Max(0.1f, floorDistance);
        public Vector2 IntroPosition => Center + Vector2.up * Mathf.Max(0f, introHeight);
        public Vector2 LaserLeft => Center + new Vector2(-Mathf.Max(1f, halfWidth), Mathf.Max(0f, laserHeight));
        public Vector2 LaserRight => Center + new Vector2(Mathf.Max(1f, halfWidth), Mathf.Max(0f, laserHeight));

        public Bounds GetLaserDangerBounds(int traversal)
        {
            float left = LaserLeft.x;
            float right = LaserRight.x;
            float safeWidth = (right - left) * Mathf.Clamp(laserSafeWidthRatio, 0.05f, 0.45f);
            if (traversal % 2 == 0) right -= safeWidth; // 1·3회: 오른쪽 안전
            else left += safeWidth;                  // 2회: 왼쪽 안전
            return new Bounds(new Vector3((left + right) * 0.5f, (FloorY + LaserLeft.y) * 0.5f, 0f),
                new Vector3(right - left, LaserLeft.y - FloorY, 0f));
        }

        public Vector2 GetLaserStart(int traversal) => LaserTravelPoint(traversal, false);
        public Vector2 GetLaserEnd(int traversal) => LaserTravelPoint(traversal, true);

        private Vector2 LaserTravelPoint(int traversal, bool end)
        {
            Bounds danger = GetLaserDangerBounds(traversal);
            float inset = Mathf.Min(0.75f, danger.size.x * 0.2f);
            bool right = (traversal % 2 == 0) == end;
            return new Vector2(right ? danger.max.x - inset : danger.min.x + inset, danger.max.y);
        }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Center = centerAnchor != null ? (Vector2)centerAnchor.position : (Vector2)owner.transform.position;
        }

        public Vector2 ClampFlightDestination(Vector2 position)
        {
            float top = Center.y + Mathf.Max(0f, laserHeight);
            float bottom = Mathf.Min(FloorY + 1f, top);
            return new Vector2(Mathf.Clamp(position.x, LaserLeft.x, LaserRight.x), Mathf.Clamp(position.y, bottom, top));
        }
    }
}
