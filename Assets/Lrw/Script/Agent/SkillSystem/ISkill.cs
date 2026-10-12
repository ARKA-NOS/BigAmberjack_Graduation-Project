using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.SkillSystem
{
    public interface ISkill
    {
        SkillSO SkillSo { get; }
        float GetSkillStartTime { get; }
        float GetMaxCooldown();
        void InitSkill(ModuleOwner owner);
        bool CanUseSkill();
        void UseSkill();
    }
}