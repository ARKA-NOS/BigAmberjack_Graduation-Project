using System.Collections;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    //[CreateAssetMenu(fileName = "Skill Action", menuName = "Skill System/Skill Action", order = 0)]
    public abstract class AbstractSkillAction : ScriptableObject
    {
        public abstract IEnumerator SkillUse(ModuleOwner owner,ISkillPlayer skillPlayer);
    }
}