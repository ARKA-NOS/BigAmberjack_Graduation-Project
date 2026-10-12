using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem.NormalSkill
{
    public abstract class AbstractSkill : MonoBehaviour, ISkill
    {
        [field:SerializeField] public SkillSO SkillSo { get; private set; }
        
        [field: SerializeField] public float BaseCooldown { get; private set; } = 1f;
        
        public float SkillStartTime { get; private set; }
        
        private float NormalizeCooldown()
            => Mathf.Clamp01(1f - (SkillDeltaTime / GetMaxCooldown()));

        public virtual float GetMaxCooldown() => BaseCooldown;
        private float SkillDeltaTime => Time.time - _lastUseTime;
        
        private float _lastUseTime = float.NegativeInfinity;
        
        public virtual void InitSkill(ModuleOwner owner)
        {
            
        }

        public virtual bool CanUseSkill()
        {
            return NormalizeCooldown() <= 0f;
        }

        public virtual void UseSkill()
        {
            _lastUseTime = Time.time;
        }

        public virtual void StopSkill()
        {
            
        }
    }
}