using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    [CreateAssetMenu(fileName = "Skill SO", menuName = "SkillSystem/Skill SO", order = 0)]
    public class SkillSO : ScriptableObject
    {
        [field:SerializeField] public Sprite SkillImage { get; private set; }
        
        /// <summary>
        /// 다른 공격이 이걸 취소할 수 있는가?
        /// </summary>
        [field: SerializeField] public bool Cancelable { get; private set; } = false;
        
        /// <summary>
        /// 이 공격이 다른 공격을 취소할 수 있는가?
        /// </summary>
        [field: SerializeField] public bool CanInterrupt { get; private set; } = false;

        /// <summary>
        /// 어떤 공격도 이걸 취소할 수 없는가? (Cancelable, CanInterrupt보다 우선)
        /// </summary>
        [field: SerializeField] public bool Uninterruptible { get; private set; } = false;

        
    }
}