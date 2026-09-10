using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    public abstract class AbstractSkill : MonoBehaviour, ISkill
    {
        [field:SerializeField] public SkillSO SkillSo { get; private set; }
        
        public float NormalizeCooldown
            => Mathf.Clamp01(1f - ((Time.time - _lastUseTime) / GetMaxCooldown()));

        private float _lastUseTime = float.NegativeInfinity;
        
        public abstract void InitSkill(ModuleOwner owner);
        public abstract bool CanUseSkill();

        public virtual void UseSkill()
        {
            _lastUseTime = Time.time;
        }
        public abstract float GetMaxCooldown();
    }
}