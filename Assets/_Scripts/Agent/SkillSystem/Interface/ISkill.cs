using _Scripts.DevLib.ModuleSystem;

namespace _Scripts.Agent.SkillSystem.Interface
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