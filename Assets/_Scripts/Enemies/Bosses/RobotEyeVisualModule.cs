using DevLib.ModuleSystem;
using UnityEngine;

namespace CTJ.Enemies.Bosses
{
    // 패턴/발사 위치는 변경하지 않고, 눈동자의 그림과 시선만 관리합니다.
    [DisallowMultipleComponent]
    public sealed class RobotEyeVisualModule : Module
    {
        [SerializeField] private SpriteRenderer globe;
        [Tooltip("EyeGlobe 바로 아래의 눈동자 렌더러. 원형 스프라이트와 로컬 회전 0을 기준으로 합니다.")]
        [SerializeField] private SpriteRenderer iris;
        [Tooltip("Iris의 자식인 붉은 눈동자. 기본 눈동자 위에 알파로 겹칩니다.")]
        [SerializeField] private SpriteRenderer redIris;
        [SerializeField] private Sprite normalIris;
        [SerializeField] private Sprite afterLaserIris;
        [Tooltip("눈 베이스 원본 스프라이트 픽셀 기준의 안쪽 여백. 눈동자 반지름도 함께 제외합니다.")]
        [SerializeField, Min(0f)] private float paddingPixels = 5f;
        [Tooltip("눈동자가 목표 위치를 따라가는 완화 시간(초). 0이면 즉시 따라갑니다.")]
        [SerializeField, Min(0f)] private float followSmoothTime = 0.08f;

        private RobotEyeBoss _boss;
        private BossLaserSkill _laser;
        private BossArenaModule _arena;
        private Vector2 _velocity;
        public float RedBlend { get; private set; }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _boss = owner as RobotEyeBoss;
            _laser = owner.GetComponentInChildren<BossLaserSkill>(true);
            _arena = owner.GetModule<BossArenaModule>();
            if (_boss == null || globe == null || iris == null || redIris == null || globe.sprite == null ||
                normalIris == null || afterLaserIris == null || iris.transform.parent != globe.transform ||
                redIris.transform.parent != iris.transform || _laser == null || _arena == null)
            {
                Debug.LogError($"{name}: RobotEyeBoss / Globe / Iris / 자식 RedIris / 두 스프라이트 / Laser / Arena를 확인하세요.", this);
                enabled = false;
                return;
            }
            iris.sprite = normalIris;
            redIris.sprite = afterLaserIris;
            RedBlend = 0f;
            UpdateRedBlend();
            PlaceIris(globe.sprite.bounds.center);
        }

        private void LateUpdate()
        {
            if (_boss == null || !_boss.isActiveAndEnabled || _boss.IsDead || globe == null ||
                globe.sprite == null || iris == null || iris.sprite == null) return;

            UpdateRedBlend();

            Vector2 center = globe.sprite.bounds.center;
            Vector2 destination = center;
            bool firingLaser = _laser.IsFiring;
            if (firingLaser)
            {
                Vector2 direction = globe.transform.InverseTransformVector(_laser.BeamDirection);
                destination = center + direction.normalized * GetTravelRadius();
                _velocity = Vector2.zero;
            }
            else if (_boss.HasCombatTarget)
            {
                // 월드 위치를 베이스의 로컬 공간으로 옮겨 보스 이동/크기와 무관하게 계산합니다.
                Vector2 target = globe.transform.InverseTransformPoint(_boss.Target.position);
                destination = center + Vector2.ClampMagnitude(target - center, GetTravelRadius());
            }

            Vector2 current = GetIrisCenter();
            Vector2 next = !firingLaser && followSmoothTime > 0f
                ? Vector2.SmoothDamp(current, destination, ref _velocity, followSmoothTime)
                : destination;
            // 스프라이트/여백 변경이나 보간 중에도 전체 눈동자가 허용 원 안에 남게 합니다.
            PlaceIris(center + Vector2.ClampMagnitude(next - center, GetTravelRadius()));
        }

        private void UpdateRedBlend()
        {
            if (_boss.HasFinishedLaserPhase) RedBlend = 1f;
            else if (_boss.CurrentState == RobotEyeBossState.ReturnCenter)
            {
                // 베이스의 아래쪽이 아레나 위쪽에 드러날 때부터 중앙 도착까지 붉어집니다.
                float visibleY = _arena.LaserBounds.max.y + globe.bounds.extents.y;
                float progress = Mathf.InverseLerp(visibleY, _arena.Center.y, _boss.transform.position.y);
                RedBlend = Mathf.Max(RedBlend, Mathf.SmoothStep(0f, 1f, progress));
            }
            Color color = redIris.color;
            color.a = RedBlend;
            redIris.color = color;
        }

        private float GetTravelRadius()
        {
            Vector3 extent = globe.sprite.bounds.extents;
            Vector3 irisExtent = Vector3.Scale(iris.sprite.bounds.extents, iris.transform.localScale);
            Vector3 redExtent = Vector3.Scale(afterLaserIris.bounds.extents,
                Vector3.Scale(iris.transform.localScale, redIris.transform.localScale));
            float globeRadius = Mathf.Min(extent.x, extent.y);
            float irisRadius = Mathf.Max(Mathf.Abs(irisExtent.x), Mathf.Abs(irisExtent.y));
            irisRadius = Mathf.Max(irisRadius, Mathf.Max(Mathf.Abs(redExtent.x), Mathf.Abs(redExtent.y)));
            return Mathf.Max(0f, globeRadius - irisRadius - Mathf.Max(0f, paddingPixels) / globe.sprite.pixelsPerUnit);
        }

        private Vector2 GetIrisCenter() => (Vector2)iris.transform.localPosition +
            (Vector2)Vector3.Scale(iris.sprite.bounds.center, iris.transform.localScale);

        private void PlaceIris(Vector2 center)
        {
            Vector3 pivotOffset = Vector3.Scale(iris.sprite.bounds.center, iris.transform.localScale);
            iris.transform.localPosition = new Vector3(center.x - pivotOffset.x, center.y - pivotOffset.y,
                iris.transform.localPosition.z);
            Vector3 redCenter = Vector3.Scale(afterLaserIris.bounds.center, redIris.transform.localScale);
            redIris.transform.localPosition = new Vector3(iris.sprite.bounds.center.x - redCenter.x,
                iris.sprite.bounds.center.y - redCenter.y, redIris.transform.localPosition.z);
        }

        private void OnDisable() => _velocity = Vector2.zero;
    }
}
