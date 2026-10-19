using Lrw.Script.Agent.SkillSystem.Interface;
using Lrw.Script.UI.BindSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.Skill_UI
{
    public class PlayerSkillUIBinder : AbstractUIBinder<ISkill>
    {
        [SerializeField] private Image skillImage;
        [SerializeField] private Image coolTimeImage;
        [SerializeField] private Image blockImage;
        
        
        protected override void DataUpdate()
        {
            CoolTimeImageUpdate();
            BlockImageUpdate();
        }
        
        private void CoolTimeImageUpdate()
        {
            coolTimeImage.fillAmount = CurrentValue is IHasCoolTime coolTime 
                ? coolTime.RemainingCoolTime / coolTime.CoolTime 
                : 0f;
        }

        private void BlockImageUpdate()
        {
            blockImage.gameObject.SetActive(CurrentValue?.CanUseSkill() ?? false);
        }
        
    }
}