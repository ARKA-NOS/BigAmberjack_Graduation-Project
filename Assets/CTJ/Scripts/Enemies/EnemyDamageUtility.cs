using DevLib.BattleSystem;
using UnityEngine;

namespace CTJ.Enemies
{
    // 두 공격 방식에서 같은 타깃/부모 피격 컴포넌트 판정을 사용합니다.
    internal static class EnemyDamageUtility
    {
        internal static bool IsTargetCollider(Collider2D hit, Transform target)
        {
            return hit != null && target != null && target.gameObject.activeInHierarchy &&
                   (hit.transform == target || hit.transform.IsChildOf(target));
        }

        internal static bool TryApply(Collider2D hit, Transform target, EnemyBase dealer,
            float amount, Vector2 point, Vector2 direction)
        {
            if (!IsTargetCollider(hit, target))
                return false;

            IDamageable receiver = hit.GetComponentInParent<IDamageable>();
            if (receiver == null)
                return false;

            Vector2 hitDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            DamageData data = new DamageData
            {
                Dealer = dealer,
                DamageAmount = Mathf.Max(0f, amount),
                DirectedKBForce = Vector2.zero
            };
            receiver.ApplyDamage(data, point, hitDirection, -hitDirection);
            return true;
        }
    }
}
