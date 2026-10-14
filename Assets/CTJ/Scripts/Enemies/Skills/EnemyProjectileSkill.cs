using UnityEngine;

namespace CTJ.Enemies.Skills
{
    public sealed class EnemyProjectileSkill : EnemyAttackSkill
    {
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField, Min(0f)] private float damage = 5f;
        [Tooltip("선택 사항. 발사 순간 재생하며, 발사 자체는 이벤트를 기다리지 않습니다.")]
        [SerializeField] private Animator attackAnimator;
        [SerializeField] private string attackStateName = "Base Layer.Attack";
        [Tooltip("발사 후 다른 스킬/이동까지 대기하는 시간. 애니메이션 길이에 맞춥니다.")]
        [SerializeField, Min(0f)] private float recoveryTime = 0.3f;
        private float _endTime;
        private Vector2 _castDirection;

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() &&
            projectilePrefab != null && projectilePrefab.gameObject.activeSelf && firePoint != null;

        protected override void PrepareAttack()
        {
            _castDirection = CastTarget.position - firePoint.position;
            if (_castDirection.sqrMagnitude < 0.0001f) _castDirection = Vector2.right;
            _castDirection.Normalize(); // 예고 방향으로 발사합니다. 준비 중 타깃을 재조준하지 않습니다.
        }

        protected override void UpdateTelegraph()
        {
            if (firePoint != null && projectilePrefab != null)
                Telegraph.SetLane(firePoint.position, _castDirection, projectilePrefab.TravelDistance, projectilePrefab.Diameter);
        }

        protected override void OnReleaseAttack()
        {
            Telegraph?.Hide();
            if (projectilePrefab == null || firePoint == null) { StopSkill(); return; }
            EnemyProjectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            projectile.Launch(Enemy, CastTarget, damage, _castDirection);
            int stateHash = Animator.StringToHash(attackStateName);
            if (attackAnimator != null && attackAnimator.isActiveAndEnabled &&
                attackAnimator.runtimeAnimatorController != null && attackAnimator.HasState(0, stateHash))
            {
                attackAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                attackAnimator.Play(stateHash, 0, 0f);
            }
            _endTime = Time.time + Mathf.Max(0f, recoveryTime);
            if (recoveryTime <= 0f) EndSkill();
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (IsUsing && HasReleased && Time.time >= _endTime) EndSkill();
        }
    }
}
