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
        [Tooltip("화면 밖 경유점 계산용. 비워두면 Main Camera를 사용합니다.")]
        [SerializeField] private Camera arenaCamera;
        [Tooltip("화면/아레나 경계 바깥의 여유 거리. 보스 크기는 별도로 더합니다.")]
        [SerializeField, Min(0.1f)] private float offscreenPadding = 1f;

        public Vector2 Center { get; private set; }
        public float FloorY => Center.y - Mathf.Max(0.1f, floorDistance);
        public Vector2 IntroPosition => Center + Vector2.up * Mathf.Max(0f, introHeight);
        public Vector2 LaserLeft => Center + new Vector2(-Mathf.Max(1f, halfWidth), Mathf.Max(0f, laserHeight));
        public Vector2 LaserRight => Center + new Vector2(Mathf.Max(1f, halfWidth), Mathf.Max(0f, laserHeight));

        public Bounds LaserBounds => new Bounds(new Vector3(Center.x, (FloorY + LaserLeft.y) * 0.5f, 0f),
            new Vector3(LaserRight.x - LaserLeft.x, LaserLeft.y - FloorY, 0f));
        // 중심을 벽의 내측 경계에 맞추고, 바깥 절반은 벽의 렌더링으로 가립니다.
        public Vector2 GetLaserStart(int traversal) => traversal % 2 == 0 ? LaserLeft : LaserRight;
        public Vector2 GetLaserEnd(int traversal) => traversal % 2 == 0 ? LaserRight : LaserLeft;

        public Vector2 GetOutsidePoint(bool right, float bodyRadius, float brakingDistance = 0f)
        {
            Bounds view = GetViewBounds();
            float margin = Mathf.Max(0f, bodyRadius) + Mathf.Max(0.1f, offscreenPadding) + Mathf.Max(0f, brakingDistance);
            return new Vector2(right ? Mathf.Max(LaserRight.x, view.max.x) + margin :
                Mathf.Min(LaserLeft.x, view.min.x) - margin, LaserLeft.y);
        }

        public float GetAboveScreenY(float bodyRadius) => Mathf.Max(IntroPosition.y, GetViewBounds().max.y) +
            Mathf.Max(0f, bodyRadius) + Mathf.Max(0.1f, offscreenPadding);

        private Bounds GetViewBounds()
        {
            Camera camera = arenaCamera != null ? arenaCamera : Camera.main;
            Bounds arena = LaserBounds;
            if (camera == null) return arena;
            float depth = Vector3.Dot((Vector3)Center - camera.transform.position, camera.transform.forward);
            if (depth <= 0f) return arena;
            Bounds view = new Bounds(camera.ViewportToWorldPoint(new Vector3(0f, 0f, depth)), Vector3.zero);
            for (int x = 0; x <= 1; x++)
                for (int y = 0; y <= 1; y++)
                    view.Encapsulate(camera.ViewportToWorldPoint(new Vector3(x, y, depth)));
            // 이미 화면과 떨어진 다른 방/테스트 보스를 카메라가 있는 방까지 이동시키지 않습니다.
            if (view.max.x < arena.min.x || view.min.x > arena.max.x || view.max.y < arena.min.y || view.min.y > arena.max.y)
                return arena;
            arena.Encapsulate(view);
            return arena;
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
