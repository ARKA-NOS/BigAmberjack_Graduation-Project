using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem.ReuseSkill
{
    [CreateAssetMenu(fileName = "Reuse Skill SO", menuName = "SkillSystem/Reuse Skill SO", order = 0)]
    public class ReuseSkillSO : ScriptableObject
    {
        [field: SerializeField] public float FirstDelay { get; private set; } = 0.5f;
        [field: SerializeField] public float CanUseTime { get; private set; } = 3f;
        
    }
}