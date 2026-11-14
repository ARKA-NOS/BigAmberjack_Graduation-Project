using UnityEngine;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class EnemyShieldBashSkill : EnemyTimedMeleeSkill
    {
        [SerializeField, Min(0f)] private float preparationStepBack = 0.28f;
        [SerializeField, Min(0f)] private float stepBackSpeed = 1.8f;

        private EnemyMovementModule _movement;
        private bool _steppingBack;
        private float _stepBackX;

        public override bool ControlsMovement => IsPreparing && _steppingBack;
        internal override bool InterruptibleByHit => IsUsing && !IsRecovering;

        protected override void PrepareAttack()
        {
            base.PrepareAttack();
            _movement = Enemy.GetModule<EnemyMovementModule>();
            _steppingBack = _movement != null && preparationStepBack > 0f && stepBackSpeed > 0f;
            _stepBackX = Enemy.transform.position.x - Enemy.FacingDirection * preparationStepBack;
        }

        public override void FixedUpdateAttack()
        {
            _steppingBack = _movement.MoveOnGroundTowards(_stepBackX, stepBackSpeed, out _) != 0f;
        }

        protected override void OnReleaseAttack()
        {
            StopStepBack();
            base.OnReleaseAttack();
        }

        protected override void OnStopSkill()
        {
            StopStepBack();
            base.OnStopSkill();
        }

        private void StopStepBack()
        {
            if (_steppingBack) _movement.Stop();
            _steppingBack = false;
        }
    }
}
