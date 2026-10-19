using UnityEngine;

namespace Lrw.Script.Agent
{
    public interface IRenderer
    {
        Animator Animator { get; }
        void PlayClip(int clipHash, int layerIndex = 0);
        void SetAnimatorFloat(int idHash, float value);
        void SetFlip(float dir);
        Vector3 GetRight();
    }
}