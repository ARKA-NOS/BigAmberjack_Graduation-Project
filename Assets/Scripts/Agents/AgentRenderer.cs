
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

        public void SetAnimatorFloat(int idHash, float value)
        {
            Animator.SetFloat(idHash, value);
        }

        public void SetFlip(float dir)
        {
            if(Mathf.Approximately(dir,0f)) return;
            transform.rotation = Quaternion.Euler(0f, dir > 0 ? 0f : 180f, 0f);
        }

        public Vector3 GetRight()
            => transform.right;
    }
}