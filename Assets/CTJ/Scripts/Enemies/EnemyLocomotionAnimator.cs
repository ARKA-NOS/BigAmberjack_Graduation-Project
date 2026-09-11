using UnityEngine;

namespace CTJ.Enemies
{
    // Visual 자식에 부착합니다. 공격 상태는 각 적이 재생하고, 이 컴포넌트는 이동 Bool만 갱신합니다.
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyLocomotionAnimator : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        [SerializeField] private EnemyBase enemy;
        private Animator _animator;
        private Rigidbody2D _body;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (enemy == null)
                enemy = GetComponentInParent<EnemyBase>();
            if (enemy != null)
                _body = enemy.GetComponent<Rigidbody2D>();
        }

        private void LateUpdate()
        {
            if (enemy == null || _body == null || !_animator.isActiveAndEnabled)
                return;

            bool isMoving = enemy.isActiveAndEnabled && !enemy.IsAttackInProgress &&
                            Mathf.Abs(_body.linearVelocityX) > 0.01f;
            _animator.SetBool(IsMovingHash, isMoving);
        }
    }
}
