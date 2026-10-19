using Lrw.Script.Agent.SkillSystem.Interface;
using Lrw.Script.UI.BindSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HDU_UI.Skill_UI
{
    public class PlayerSkillUIBinder : AbstractUIBinder<ISkill>
    {
        [SerializeField] private Image skillImage;
        [SerializeField] private Image coolTimeImage;
        [SerializeField] private Image blockImage;
        
        
        protected override void DataUpdate()
        {
            if (CurrentValue is IHasCoolTime coolTime)
            {
                
            }
            else
            {
                
            }
            //coolTimeImage.fillAmount = CurrentValue.
            
        }
        
        
        
        
    }
}