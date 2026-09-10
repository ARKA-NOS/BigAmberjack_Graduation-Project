using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    //[CreateAssetMenu(fileName = "Skill Condition", menuName = "Skill System/Skill Condition", order = 0)]
    public abstract class AbstractSkillUseCondition : ScriptableObject
    {
        public abstract bool UseCondition(ModuleOwner owner,ISkillPlayer skillPlayer);
        
        
        
    }
}