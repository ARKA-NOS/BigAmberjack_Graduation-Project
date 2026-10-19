using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.Interface;

namespace Lrw.Script.Agent.SkillSystem
{
    public interface ISkillModule
    {
        ModuleOwner Owner { get; }
        ISkill GetSkill(SkillSO skill);
        public bool CanUseSkill(SkillSO skillSo);
        public void UseSkill(SkillSO skillSo, bool keyPressed);
        public void ForceStopCurrentSkill();
    }
}
