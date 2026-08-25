using UnityEngine;

namespace Agents
{
    public interface IRenderer
    {
        Animator Animator { get; }
        void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0);
        public void SetFloat(int idHash, float value);
    }
}