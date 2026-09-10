using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    [CreateAssetMenu(fileName = "Skill SO", menuName = "SkillSystem/Skill SO", order = 0)]
    public class SkillSO : ScriptableObject
    {
        [field: SerializeField] public float BaseCooldown { get; private set; } = 10f;
        
        
    }
}