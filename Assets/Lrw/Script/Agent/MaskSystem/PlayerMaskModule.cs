using System.Collections.Generic;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.MaskSystem
{
    public class PlayerMaskModule : Module
    {
        private Dictionary<MaskSo,IMask> _maskDict = new();
        private IMask _currentMask;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _maskDict = GetSkillPlayers(GetComponentsInChildren<IMask>(true));
        }
        
        private Dictionary<MaskSo, IMask> GetSkillPlayers(IMask[] skillPlayers)
        {
            Dictionary<MaskSo, IMask> maskDict = new();

            foreach (IMask mask in skillPlayers)
            {
                if (mask.MaskSo == null)
                {
                    FDebug.LogError($"[{mask}] Mask SO가 null 입니다.");
                    continue;
                }

                if (!maskDict.TryAdd(mask.MaskSo, mask))
                {
                    FDebug.LogError($"[{mask}] 같은 Mask SO가 있습니다.");
                    continue;
                }
                
                mask.Init(_owner);
            }

            return maskDict;
        }

        public void ChangeMask(MaskSo maskSo)
        {
            _currentMask?.Exit();
            _currentMask = null;

            if (maskSo != null && _maskDict.TryGetValue(maskSo, out _currentMask))
            {
                _currentMask.Enter();
            }
            
        }
        
        private void Update()
        {
            _currentMask?.Update();
        }
    }
}