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

        public Vector2 Center { get; private set; }
        public float FloorY => Center.y - Mathf.Max(0.1f, floorDistance);
        public Vector2 IntroPosition => Center + Vector2.up * Mathf.Max(0f, introHeight);
        public Vector2 LaserLeft => Center + new Vector2(-Mathf.Max(1f, halfWidth), Mathf.Max(0f, laserHeight));
        public Vector2 LaserRight => Center + new Vector2(Mathf.Max(1f, halfWidth), Mathf.Max(0f, laserHeight));

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
