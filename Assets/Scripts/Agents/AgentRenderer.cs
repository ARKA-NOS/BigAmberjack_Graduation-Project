
using DevLib.ModuleSystem;
using UnityEngine;

namespace Agents
{
    public class AgentRenderer : Module, IRenderer
    {
        public Animator Animator { get; private set; }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Animator = GetComponent<Animator>();
        }

        public void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0)
        {
            Animator.CrossFadeInFixedTime(clipHash, crossFadeDuration, layerIndex, normalizedTime);
        }

        public void SetFloat(int idHash, float value)
        {
            Animator.SetFloat(idHash, value);
        }
    }
}