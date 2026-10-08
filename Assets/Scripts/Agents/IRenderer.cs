using UnityEngine;

namespace Agents
{
    public interface IRenderer
    {
        Animator Animator { get; }
        void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0);
        void SetAnimatorFloat(int idHash, float value);
        void SetFlip(float dir);
        Vector3 GetRight();
    }
}