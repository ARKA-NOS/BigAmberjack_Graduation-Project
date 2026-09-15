using CoreSystem.Effect;
using CoreSystem.EffectSystem;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Agents.Players
{
    public class PlayerMultiJumper : Module, IControlMultiJumper,IAfterInitModule
    {
        [SerializeField] private bool activeDoubleJump = true;
        [SerializeField] private StatData multiJumpCountStatData;
        [SerializeField] private AssetNameSo multiJumpVfx; 
        
        private IGroundChecker _groundChecker;
        private IVfxModule _vfxModule;
        private IStatModule _statModule;
        private Stat _multiJumpCount;
        
        private int _currentJumpCount = 0;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _groundChecker = owner.GetModule<IGroundChecker>();
            _vfxModule = owner.GetModule<IVfxModule>();
            _statModule = owner.GetModule<IStatModule>();
        }

        public void AfterInit()
        {
            _multiJumpCount = _statModule.GetStat(multiJumpCountStatData, 1f);
        }

        public bool CanDoubleJump()
        {
            if (_groundChecker.IsGroundChecking() || _currentJumpCount  > _multiJumpCount.Value - 1 || !activeDoubleJump) return false;
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