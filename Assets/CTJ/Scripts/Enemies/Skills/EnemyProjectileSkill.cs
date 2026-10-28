using UnityEngine;

namespace CTJ.Enemies.Skills
{
    public sealed class EnemyProjectileSkill : EnemyAttackSkill
    {
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [Tooltip("발사체 이동 속도(월드 거리/초). 0이면 Projectile Prefab의 Speed를 사용합니다.")]
        [SerializeField, Min(0f)] private float projectileSpeed;
        [Tooltip("발사체 유지 시간(초). 0이면 Projectile Prefab의 Lifetime을 사용합니다.")]
        [SerializeField, Min(0f)] private float projectileLifetime;
        [SerializeField, Min(0f)] private float damage = 5f;
        [Tooltip("선택 사항. 발사 순간 재생하며, 발사 자체는 이벤트를 기다리지 않습니다.")]
        [SerializeField] private Animator attackAnimator;
        [SerializeField] private string attackStateName = "Base Layer.Attack";
        [Tooltip("선택 사항. 준비 시작 시 재생하고 마지막 자세를 유지하는 상태입니다.")]
        [SerializeField] private string preparationStateName = "";
        [Tooltip("선택 사항. 공격 종료/취소 때 돌아갈 상태입니다. 비우면 기존 Animator 전환을 사용합니다.")]
        [SerializeField] private string idleStateName = "";
        [Tooltip("발사 후 다른 스킬/이동까지 대기하는 시간(초). 터렛은 이 시간 동안 발사 방향과 전개 상태를 유지합니다.")]
        [SerializeField, Min(0f)] private float recoveryTime = 0.3f;
        [Header("Aim")]
        [Tooltip("준비 중 조준 방향에 맞춰 몸체도 좌우 반전합니다. 조준 고정 후에는 방향을 바꾸지 않습니다.")]
        [SerializeField] private bool faceTargetDuringPreparation;
        [Tooltip("준비 중 조준선 표시만 켜고 끕니다. 실제 발사 방향의 추적과는 별개입니다.")]
        [SerializeField] private bool showAimLine = true;
        [Tooltip("준비 중 타깃을 따라 조준하다가 마지막 Post Blink Delay에 실제 포신 방향을 고정합니다. 끄면 준비 시작의 목표 방향으로만 조준합니다.")]
        [SerializeField] private bool trackTargetDuringPreparation;
        [SerializeField] private bool aimAtTargetCenter;
        [Tooltip("0이면 기존처럼 투사체 지름을 사용합니다. 조준선용 프리팹은 얇은 값을 지정할 수 있습니다.")]
        [SerializeField, Min(0f)] private float aimLineWidth;
        [Tooltip("선택 사항. 전개 완료 후 공격하며, 발사기의 회전을 발사 방향과 일치시킵니다.")]
        [SerializeField] private EnemyTurretModule turret;
        private float _endTime;
        private Vector2 _castDirection;
        private Vector2 _desiredAimDirection;
        private bool _aimLocked;

        public Vector2 CastDirection => _castDirection;
        public bool IsAimLocked => _aimLocked;
        private float ProjectileSpeed => projectileSpeed > 0f ? Mathf.Max(0.01f, projectileSpeed) : projectilePrefab.Speed;
        private float ProjectileLifetime => projectileLifetime > 0f ? Mathf.Max(0.01f, projectileLifetime) : projectilePrefab.Lifetime;
        protected override bool ShouldShowTelegraph => showAimLine;

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() &&
            projectilePrefab != null && projectilePrefab.gameObject.activeSelf && firePoint != null &&
            (turret == null || turret.IsDeployed);

        protected override void PrepareAttack()
        {
            _aimLocked = turret == null && !trackTargetDuringPreparation;
            if (faceTargetDuringPreparation) Enemy.FaceTarget();
            UpdateAimTarget();
            RotateAim();
            PlayAnimation(preparationStateName);
        }

        private void UpdateAimTarget()
        {
            if (CastTarget == null || firePoint == null) return;
            Vector2 targetPoint = CastTarget.position;
            if (aimAtTargetCenter)
            {
                Collider2D targetBody = CastTarget.GetComponent<Collider2D>();
                if (targetBody != null && targetBody.enabled) targetPoint = targetBody.bounds.center;
            }
            Vector2 origin = turret != null ? turret.AimOrigin : (Vector2)firePoint.position;
            _desiredAimDirection = targetPoint - origin;
            if (_desiredAimDirection.sqrMagnitude < 0.0001f) _desiredAimDirection = Vector2.right;
            _desiredAimDirection.Normalize();
        }

        private void RotateAim()
        {
            if (turret != null)
            {
                turret.SetAimDirection(_desiredAimDirection);
                _castDirection = turret.AimDirection;
            }
            else _castDirection = _desiredAimDirection;
        }

        protected override void UpdateTelegraph()
        {
            if (firePoint != null && projectilePrefab != null)
                Telegraph.SetLane(firePoint.position, _castDirection, ProjectileSpeed * ProjectileLifetime,
                    aimLineWidth > 0f ? aimLineWidth : projectilePrefab.Diameter);
        }

        protected override void OnReleaseAttack()
        {
            Telegraph?.Hide();
            if (projectilePrefab == null || firePoint == null || (turret != null && !turret.IsDeployed)) { StopSkill(); return; }
            EnemyProjectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            // 프리팹 원본이 아닌 이번 탄에 적용합니다. 수명 종료 시각을 계산하는 Launch보다 먼저 설정합니다.
            projectile.Speed = ProjectileSpeed;
            projectile.Lifetime = ProjectileLifetime;
            projectile.Launch(Enemy, CastTarget, damage, _castDirection);
            PlayAnimation(attackStateName);
            _endTime = Time.time + Mathf.Max(0f, recoveryTime);
            BeginRecovery();
            if (recoveryTime <= 0f) EndSkill();
        }

        public override void UpdateSkill()
        {
            if (turret != null && !turret.isActiveAndEnabled) { StopSkill(); return; }
            if (IsPreparing && CanContinue && !_aimLocked)
            {
                if (faceTargetDuringPreparation) Enemy.FaceTarget();
                if (trackTargetDuringPreparation) UpdateAimTarget();
                else if (turret == null) _aimLocked = true;
                RotateAim();
            }
            base.UpdateSkill();
            if (IsHoldingWarning)
            {
                _aimLocked = true;
                if (turret != null) turret.HoldAim();
            }
            if (IsUsing && HasReleased && Time.time >= _endTime) EndSkill();
        }

        private void PlayAnimation(string stateName)
        {
            if (Enemy == null || Enemy.IsDead || string.IsNullOrEmpty(stateName) || attackAnimator == null ||
                !attackAnimator.isActiveAndEnabled || attackAnimator.runtimeAnimatorController == null) return;
            int stateHash = Animator.StringToHash(stateName);
            if (!attackAnimator.HasState(0, stateHash)) return;
            attackAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            attackAnimator.Play(stateHash, 0, 0f);
        }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            PlayAnimation(idleStateName);
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            PlayAnimation(idleStateName);
        }
    }
}
