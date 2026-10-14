using System;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent
{
    public class AgentRenderer : Module, IRenderer,IAnimationTrigger
    {
        public Animator Animator { get; private set; }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Animator = GetComponent<Animator>();
        }

        public void PlayClip(int clipHash, int layerIndex = 0)
        {
            Animator.Play(clipHash, layerIndex);
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


        #region IAnimationTrigger
        public event Action OnAnimationEnd;
        public event Action OnAttackTrigger;
        
        public void AnimationEnd() => OnAnimationEnd?.Invoke();
        public void AttackTrigger() => OnAttackTrigger?.Invoke();
        
        #endregion
        
    }
}