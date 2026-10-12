using Lrw.Script.Agent.SkillSystem.Interface;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem.NormalSkill
{
    public abstract class AbstractCoolTimeSkill : AbstractSkill,IHasCoolTime
    {
        [SerializeField] private float baseCoolTime = 1f;

        public float SkillStartTime { get; protected set; }
        public virtual float CoolTime => baseCoolTime;

        public override void UseSkill()
        {
            base.UseSkill();
            SkillStartTime = Time.time;
        }
        
        
    }
}