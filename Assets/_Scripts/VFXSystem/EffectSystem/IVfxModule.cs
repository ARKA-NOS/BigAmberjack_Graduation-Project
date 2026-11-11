using UnityEngine;

namespace _Scripts.VFXSystem.EffectSystem
{
    public interface IVfxModule
    {
        void PlayVfx(int vfxHash, Vector3 position, Quaternion rotation);
        void PlayVfx(int vfxHash);
        void StopVfx(int vfxHash);
    }
}