using UnityEngine;

namespace _Scripts.Agent
{
    public interface IRenderer
    {
        Animator Animator { get; }
        void PlayClip(int clipHash, int layerIndex = 0, float normalizedTime = 0f);
        void SetAnimatorFloat(int idHash, float value);
        void SetFlip(float dir);
        Vector3 GetRight();
    }
}