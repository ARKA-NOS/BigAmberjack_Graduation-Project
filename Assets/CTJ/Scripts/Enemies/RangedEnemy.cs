using DevLib.BattleSystem;
using UnityEngine;

namespace CTJ.Enemies
{
    public sealed class RangedEnemy : EnemyBase
    {
        private static readonly int AttackStateHash = Animator.StringToHash("Base Layer.Attack");

        [Header("Ranged Attack")]
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField, Min(0f)] private float damage = 5f;
        [Tooltip("선택 사항. 발사 순간 Base Layer.Attack 애니메이션을 재생합니다.")]
        [SerializeField] private Animator attackAnimator;

        private bool _configurationWarningShown;

        private void Reset()
        {
            detectionRange = 10f;
            attackRange = 7f;
        }

        protected override void Attack()
        {
            if (Target == null)
                return;

            if (projectilePrefab == null || !projectilePrefab.gameObject.activeSelf || firePoint == null)
            {
                if (!_configurationWarningShown)
                {
                    Debug.LogWarning($"{name}: 활성 Projectile Prefab과 Fire Point를 지정하세요.", this);
                    _configurationWarningShown = true;
                }
                return;
            }

            Vector2 direction = (Vector2)(Target.position - firePoint.position);
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;

            EnemyProjectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            projectile.Launch(this, Target, damage, direction);

            if (attackAnimator != null && attackAnimator.isActiveAndEnabled &&
                attackAnimator.runtimeAnimatorController != null && attackAnimator.HasState(0, AttackStateHash))
                attackAnimator.Play(AttackStateHash, 0, 0f);
        }

        public override void ApplyDamage(DamageData damageData, Vector2 hitPoint,
            Vector2 hitDirection, Vector2 hitNormal)
        {
            OnHit?.Invoke();
        }
    }
}
