using DevLib.ModuleSystem;
using UnityEngine;

namespace CTJ.Enemies
{
    // 루트 물리는 고정하고 비주얼 전개/발사기 회전만 담당합니다. 공격 타이머와 피해는 스킬이 관리합니다.
    [DisallowMultipleComponent]
    public sealed class EnemyTurretModule : Module
    {
        [SerializeField] private Transform riseRoot;
        [SerializeField] private Transform launcherPivot;
        [SerializeField] private GameObject launcherVisual;
        [SerializeField] private BoxCollider2D hurtbox;
        [Tooltip("완전히 올라온 상태의 RiseRoot 로컬 위치.")]
        [SerializeField] private Vector3 deployedLocalPosition;
        [Tooltip("대기할 때 내려가는 로컬 거리. 기본 몸통 높이 1.2의 절반은 0.6입니다.")]
        [SerializeField, Min(0f)] private float buriedDepth = 0.6f;
        [Tooltip("대기 자세에서 전개 완료까지의 시간(초). 클수록 천천히 나옵니다.")]
        [SerializeField, Min(0f)] private float emergeDuration = 0.5f;
        [Tooltip("완전히 전개된 자세에서 대기 자세로 복귀하는 시간(초). 클수록 천천히 들어갑니다.")]
        [SerializeField, Min(0f)] private float retractDuration = 0.6f;
        [Header("Launcher Rotation")]
        [Tooltip("런처의 최대 회전 속도(도/초). 조준과 아래로 접는 동작에 모두 적용합니다.")]
        [SerializeField, Min(1f)] private float maxRotationSpeed = 180f;
        [Tooltip("회전이 목표 방향에 부드럽게 수렴하는 시간. 클수록 반응이 완만합니다.")]
        [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.15f;
        private const float StowedAngle = -90f;
        private EnemyBase _enemy;
        private float _deployment;
        private float _angularVelocity;
        private int _lastRotationFrame = -1;
        private Vector2 _fullSize;
        private Vector2 _fullOffset;

        public bool IsDeployed => isActiveAndEnabled && _enemy != null && _enemy.isActiveAndEnabled &&
            !_enemy.IsDead && riseRoot != null && launcherPivot != null && _deployment >= 1f;
        public float Deployment => _deployment;
        public Vector2 AimOrigin => launcherPivot != null ? (Vector2)launcherPivot.position : (Vector2)transform.position;
        public Vector2 AimDirection => launcherPivot != null ? (Vector2)launcherPivot.right : Vector2.down;
        private bool IsStowed => launcherPivot == null || Mathf.Abs(Mathf.DeltaAngle(launcherPivot.eulerAngles.z, StowedAngle)) <= 0.1f;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            if (hurtbox != null) { _fullSize = hurtbox.size; _fullOffset = hurtbox.offset; }
            _deployment = 0f;
            ResetLauncher();
            ApplyPose();
        }

        private void Update()
        {
            if (_enemy == null || !_enemy.isActiveAndEnabled || _enemy.IsDead || _enemy.IsHitReacting)
            {
                HoldAim();
                return;
            }
            // 공격 중의 회전은 스킬만 요청합니다. 마지막 경고/발사 후 회수에는 방향을 유지합니다.
            if (_enemy.IsAttackInProgress) return;
            if (_enemy.IsTargetInRange(_enemy.AttackRange))
            {
                if (_deployment < 1f)
                {
                    RotateTowards(StowedAngle);
                    if (IsStowed) MoveDeployment(1f, emergeDuration);
                    ApplyPose();
                    return;
                }
                Collider2D body = _enemy.Target.GetComponent<Collider2D>();
                Vector2 point = body != null && body.enabled ? (Vector2)body.bounds.center : (Vector2)_enemy.Target.position;
                SetAimDirection(point - AimOrigin);
            }
            else
            {
                // 포신이 아래로 접힐 때까지 몸통 높이를 유지한 다음 함께 내려갑니다.
                RotateTowards(StowedAngle);
                if (IsStowed) MoveDeployment(0f, retractDuration);
            }
            ApplyPose();
        }

        private void MoveDeployment(float destination, float duration)
        {
            _deployment = Mathf.MoveTowards(_deployment, destination,
                duration > 0f ? Time.deltaTime / duration : 1f);
        }

        public void SetAimDirection(Vector2 direction)
        {
            if (IsDeployed && !_enemy.IsHitReacting && direction.sqrMagnitude > 0.0001f)
                RotateTowards(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        public void HoldAim() => _angularVelocity = 0f;

        private void RotateTowards(float targetAngle)
        {
            // 대기 Update와 스킬 시작이 같은 프레임에 겹쳐도 최대 속도가 두 번 적용되지 않게 합니다.
            if (launcherPivot == null || Time.deltaTime <= 0f || _lastRotationFrame == Time.frameCount) return;
            _lastRotationFrame = Time.frameCount;
            float current = launcherPivot.eulerAngles.z;
            float speed = Mathf.Max(1f, maxRotationSpeed);
            float next = Mathf.SmoothDampAngle(current, targetAngle, ref _angularVelocity,
                Mathf.Max(0.01f, rotationSmoothTime), speed, Time.deltaTime);
            float maxStep = speed * Time.deltaTime;
            float step = Mathf.Clamp(Mathf.DeltaAngle(current, next), -maxStep, maxStep);
            // 목표 근처에서 무한히 수렴하지 않도록, 속도 제한 안의 작은 잔여 각도만 마무리합니다.
            float remaining = Mathf.DeltaAngle(current, targetAngle);
            if (Mathf.Abs(remaining) <= Mathf.Min(0.1f, maxStep))
            {
                step = remaining;
                HoldAim();
            }
            launcherPivot.rotation = Quaternion.Euler(0f, 0f, current + step);
        }

        private void ResetLauncher()
        {
            HoldAim();
            _lastRotationFrame = -1;
            if (launcherPivot != null) launcherPivot.rotation = Quaternion.Euler(0f, 0f, StowedAngle);
        }

        private void ApplyPose()
        {
            float remainingDepth = Mathf.Max(0f, buriedDepth) * (1f - Mathf.SmoothStep(0f, 1f, _deployment));
            if (riseRoot != null) riseRoot.localPosition = deployedLocalPosition + Vector3.down * remainingDepth;
            // 출현/퇴장 중 켜고 끄지 않습니다. 아래로 접힌 외형도 지면 마스크 안에서 함께 움직입니다.
            if (launcherVisual != null) launcherVisual.SetActive(true);
            if (hurtbox != null && _fullSize.y > 0f)
            {
                float visibleHeight = Mathf.Max(0.05f, _fullSize.y - remainingDepth);
                hurtbox.size = new Vector2(_fullSize.x, visibleHeight);
                hurtbox.offset = _fullOffset + Vector2.down * ((_fullSize.y - visibleHeight) * 0.5f);
            }
        }

        private void OnDisable()
        {
            _deployment = 0f;
            ResetLauncher();
            ApplyPose();
        }
    }
}
