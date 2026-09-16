using Lrw.Script.Agent.SkillSystem;
using UnityEditor.Animations;
using UnityEngine;

namespace Lrw.Script.Agent.MaskSystem
{
    [CreateAssetMenu(fileName = "Mask SO", menuName = "Agents/Mask SO", order = 0)]
    public class MaskSo : ScriptableObject
    {
        [field:SerializeField] public AnimatorController Controller { get; private set; }
        
        [Header("Skill Setting")]
        [field:SerializeField] public SkillSO MeleeSkill { get; private set; }
        [field:SerializeField] public SkillSO Skill1 { get; private set; }
        [field:SerializeField] public SkillSO Skill2 { get; private set; }
        [field:SerializeField] public SkillSO ChangeSkill { get; private set; }
        
    }
}