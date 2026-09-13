using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem.ReuseSkill
{
    public abstract class AbstractReuseSkill : MonoBehaviour,IReuseSkill
    {
        [field:SerializeField] public ReuseSkillSO ReuseSkillSo { get; private set; }

        public float GetMaxCooldown()
            => ReuseSkillSo.CanUseTime;
        
        public virtual float NormalizeCooldown()
        {
            return Mathf.Clamp01(1f - (SkillDeltaTime / GetMaxCooldown()));
        }

        public virtual float GetRemainingCooldown()
        {
            return Mathf.Clamp01(GetMaxCooldown() - SkillDeltaTime);
        }
        
        private float SkillDeltaTime => Time.time - _lastUseTime;
        
        private float _lastUseTime;
        
        public abstract void InitReuseSkill(ModuleOwner owner);

        public virtual bool CanUseReuseSkill()
        {
            return NormalizeCooldown() <= 0f;
        }

        public virtual void UseReuseSkill()
        {
            _lastUseTime = Time.time;
        }
    }
}