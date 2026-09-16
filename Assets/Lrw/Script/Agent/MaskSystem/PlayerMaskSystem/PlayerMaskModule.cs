using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.MaskSystem.PlayerMaskSystem
{
    public class PlayerMaskModule : MaskModule
    {
        [SerializeField] private MaskSo baseMask;
        
        private const int MaskMaxCount = 2;
        private readonly MaskSo[] _maskList = new MaskSo[MaskMaxCount];

        private int _currentMaskIndex = 0;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _maskList[0] = baseMask;
            ChangeMask(baseMask);
        }

        public void GetMask(MaskSo mask)
        {
            bool addMask = false;
            for (int i = 0; i < MaskMaxCount; i++)
            {
                if (_maskList[i] == null)
                {
                    _maskList[i] = mask;
                    addMask = true;
                    break;
                }
            }

            if (!addMask)
            {
                _maskList[_currentMaskIndex] = mask;
            }
            
            SetCurrentMask();
        }

        private void SetCurrentMask() => ChangeMask(_maskList[_currentMaskIndex]);
        
        
    }
}