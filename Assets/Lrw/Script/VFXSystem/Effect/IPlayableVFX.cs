using UnityEngine;

namespace Lrw.Script.VFXSystem.Effect
{
    public interface IPlayableVFX
    {
        AssetNameSo VfxName { get; }
        float VfxDuration { get; }
        void PlayVFX(Vector3 position, Quaternion rotation);
        void PlayVFX();
        void StopVFX();
    }
}