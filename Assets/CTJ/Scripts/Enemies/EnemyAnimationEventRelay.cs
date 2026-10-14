using UnityEngine;

namespace CTJ.Enemies
{
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyAnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private EnemyBase enemy;

        private void Awake()
        {
            if (enemy == null)
                enemy = GetComponentInParent<EnemyBase>();
        }

        public void OnAttackHit()
        {
            if (isActiveAndEnabled && enemy != null)
                enemy.OnAttackHit();
        }

        public void OnAttackFinished()
        {
            if (isActiveAndEnabled && enemy != null)
                enemy.OnAttackFinished();
        }
    }
}
