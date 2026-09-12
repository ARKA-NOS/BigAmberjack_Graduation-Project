using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    public abstract class AbstractBaseSkill : MonoBehaviour,ISkill
    {
        public virtual float NormalizeCooldown()
        {
            return Mathf.Clamp01(1f - (SkillDeltaTime / GetMaxCooldown()));
        }

        public virtual float GetRemainingCooldown()
        {
            return Mathf.Clamp01(GetMaxCooldown() - SkillDeltaTime);
        }

        public abstract float GetMaxCooldown();
        private float SkillDeltaTime => Time.time - _lastUseTime;
        
        private float _lastUseTime;
        
        public abstract void InitSkill(ModuleOwner owner);

        public virtual bool CanUseSkill()
        {
            return NormalizeCooldown() <= 0f;
        }

        public virtual void UseSkill()
        {
            _lastUseTime = Time.time;
        }
        
    }
}