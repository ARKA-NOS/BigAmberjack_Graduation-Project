using System.Collections;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    public interface ISkillPlayer
    {
        SkillSO Skill { get; }
        GameObject SkillPlayerGameObject { get; }
        void InitSkill(ISkillModule skillModule);
        bool CanUseSkill();
        void UseSkill();
    }
}