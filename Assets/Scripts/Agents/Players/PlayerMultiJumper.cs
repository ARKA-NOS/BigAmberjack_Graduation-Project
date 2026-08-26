using CoreSystem.Effect;
using CoreSystem.EffectSystem;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Agents.Players
{
    public class PlayerMultiJumper : Module, IControlMultiJumper
    {
        [SerializeField] private bool activeDoubleJump = true;
        [SerializeField] private int extraJumpCount = 1;
        [SerializeField] private AssetNameSo multiJumpVfx; 
        
        private IGroundChecker _groundChecker;
        private IVfxModule _vfxModule;
        
        private int _currentJumpCount = 0;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _groundChecker = owner.GetModule<IGroundChecker>();
            _vfxModule = owner.GetModule<IVfxModule>();
        }

        public bool CanDoubleJump()
        {
            if (_groundChecker.IsGroundChecking() || _currentJumpCount  > extraJumpCount - 1 || !activeDoubleJump) return false;
            _currentJumpCount++;
            _vfxModule.PlayVfx(multiJumpVfx.AssetHash);
            return true;
        }

        public void ResetMultiJumpCount()
        {
            _currentJumpCount = 0;
        }
    }
}