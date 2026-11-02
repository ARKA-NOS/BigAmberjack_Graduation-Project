using Lrw.Script.Agent.SkillSystem.Interface;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem.NormalSkill
{
    public abstract class AbstractCoolTimeSkill : AbstractSkill, IHasCoolTime
    {
        [field:SerializeField] protected float BaseCoolTime { get; private set; }= 1f;

        // 시작하자마자 쓸 수 있도록 아주 먼 과거로 둔다
        public float CoolTimeStartTime { get; protected set; } = float.NegativeInfinity;
        public virtual float CoolTime => BaseCoolTime;
        public float RemainingCoolTime => Mathf.Max(0f, CoolTimeStartTime + CoolTime - Time.time);

        public sealed override bool CanUseSkill()
        {
            return RemainingCoolTime <= 0f && CanUseSkillCondition();
        }

        /// <summary>
        /// 쿨타임 외에 추가로 확인할 사용 조건(자원, 지면 여부 등)
        /// </summary>
        protected virtual bool CanUseSkillCondition() => true;

        protected override void OnUseSkill()
        {
            CoolTimeStartTime = Time.time;
        }
        
    }
}
