using System.Linq;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    [CreateAssetMenu(fileName = "Skill", menuName = "Skill System/Skill", order = 0)]
    public class SkillSO : ScriptableObject
    {
        [field:SerializeField] public AbstractSkillUseCondition[] Conditions { get; private set; }
        [field:SerializeField] public AbstractSkillAction[] Actions { get; private set; }
        
        
    }
}