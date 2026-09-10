using System.Collections;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    public interface ISkill
    {
        SkillSO SkillSo { get; }
        float NormalizeCooldown { get; }
        void InitSkill(ModuleOwner owner);
        bool CanUseSkill();
        void UseSkill();
        float GetMaxCooldown();
    }
}