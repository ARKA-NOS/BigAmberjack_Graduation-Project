using UnityEngine;

namespace CTJ.Enemies
{
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyAnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private MeleeEnemy enemy;

        private void Awake()
        {
            if (enemy == null)
                enemy = GetComponentInParent<MeleeEnemy>();
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
