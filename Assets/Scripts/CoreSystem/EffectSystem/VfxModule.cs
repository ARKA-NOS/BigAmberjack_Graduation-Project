using System.Collections.Generic;
using System.Linq;
using CoreSystem.Effect;
using DevLib.ModuleSystem;
using UnityEngine;

namespace CoreSystem.EffectSystem
{
    public class VfxModule : Module, IVfxModule
    {
        private Dictionary<int, IPlayableVFX> _vfxDict;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _vfxDict = GetComponentsInChildren<IPlayableVFX>().ToDictionary(vfx => vfx.VfxName.AssetHash);
        }

        public void PlayVfx(int vfxHash, Vector3 position, Quaternion rotation)
        {
            if (_vfxDict.TryGetValue(vfxHash, out IPlayableVFX vfx))
            {
                vfx.PlayVFX(position, rotation);
            }
            else
            {
                Debug.LogWarning($"VFX with hash {vfxHash} not found");
            }
        }

        public void PlayVfx(int vfxHash)
        {
            if (_vfxDict.TryGetValue(vfxHash, out IPlayableVFX vfx))
            {
                vfx.PlayVFX(); //제자리에서 재생하는 vfx
            }
            
            else
            {
                Debug.LogWarning($"VFX with hash {vfxHash} not found");
            }
        }

        public void StopVfx(int vfxHash)
        {
            if (_vfxDict.TryGetValue(vfxHash, out IPlayableVFX vfx))
            {
                vfx.StopVFX();
            }
        }
    }
}