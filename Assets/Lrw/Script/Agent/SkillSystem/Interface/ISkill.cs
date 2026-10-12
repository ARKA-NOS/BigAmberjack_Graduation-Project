using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.SkillSystem.Interface
{
    public interface ISkill
    {
        SkillSO SkillSo { get; }
        bool IsUsing { get; }
        void InitSkill(ModuleOwner owner);
        bool CanUseSkill();
        void UseSkill();
        void StopSkill();

    }
}