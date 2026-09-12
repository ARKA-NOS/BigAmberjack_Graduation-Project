using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem.NormalSkill
{
    public abstract class AbstractNormalSkill : MonoBehaviour, INormalSkill
    {
        [field:SerializeField] public SkillSO SkillSo { get; private set; }
        
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
        
        private float _lastUseTime = float.NegativeInfinity;
        
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