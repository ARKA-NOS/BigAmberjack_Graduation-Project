using Lrw.Script.Agent.SkillSystem;

namespace Lrw.Script.Agent.Players
{
    public interface IPlayerSkillModule : ISkillModule
    {
        void SetSkill(SkillSlot slot,SkillSO skill);
    }
}