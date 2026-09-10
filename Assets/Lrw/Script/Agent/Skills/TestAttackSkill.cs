using System.Collections;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Skills
{
    [CreateAssetMenu(fileName = "Test Skill Action", menuName = "Skill System/Test Skill Action", order = 0)]
    public class TestAttackSkill : AbstractSkillAction
    {
        public override IEnumerator SkillUse(ModuleOwner owner, ISkillPlayer skillPlayer)
        {
            for (int i = 0; i < 5; i++)
            {
                FDebug.Log(i);
                yield return new WaitForSeconds(1);
            }
        }
        
    }
}