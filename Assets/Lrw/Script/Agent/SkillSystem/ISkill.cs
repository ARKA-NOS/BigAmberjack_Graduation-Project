using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.SkillSystem
{
    public interface ISkill
    {
        SkillSO SkillSo { get; }
        float SkillStartTime { get; }
        float GetMaxCooldown();
        void InitSkill(ModuleOwner owner);
        bool CanUseSkill();
        void UseSkill();
        void StopSkill();
    }
}