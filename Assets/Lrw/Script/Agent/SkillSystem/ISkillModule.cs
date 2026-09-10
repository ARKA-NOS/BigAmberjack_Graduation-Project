using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.SkillSystem
{
    public interface ISkillModule
    {
        ModuleOwner Owner { get; }

        public bool CanUseSkill(SkillSO skillSo);
        public void UseSkill(SkillSO skillSo);
    }
}