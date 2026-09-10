using Lrw.Script.Agent.SkillSystem;
using UnityEngine;

namespace Lrw.Script.Test
{
    public class SkillTester : MonoBehaviour
    {
        [SerializeField] private SkillSO skill;
        [SerializeField] private SkillModule skillModule;

        [ContextMenu("UseSkill")]
        public void UseSkill()
        {
            if (skillModule.CanUseSkill(skill))
            {
                skillModule.UseSkill(skill);
            }
            
        }
    }
}