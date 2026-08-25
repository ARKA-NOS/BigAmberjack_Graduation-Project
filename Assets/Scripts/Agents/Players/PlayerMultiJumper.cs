using DevLib.ModuleSystem;
using UnityEngine;

namespace Agents.Players
{
    public class PlayerMultiJumper : Module, IControlMultiJumper
    {
        [SerializeField] private bool activeDoubleJump = true;
        [SerializeField] private int extraJumpCount = 1;
        
        private IGroundChecker _groundChecker;

        private int _currentJumpCount = 0;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _groundChecker = owner.GetModule<IGroundChecker>();
        }

        public bool CanDoubleJump()
        {
            if (_groundChecker.IsGroundChecking() || _currentJumpCount  > extraJumpCount - 1 || !activeDoubleJump) return false;
            _currentJumpCount++;
            return true;
        }

        public void ResetMultiJumpCount()
        {
            _currentJumpCount = 0;
        }
    }
}