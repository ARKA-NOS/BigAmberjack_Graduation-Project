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
            SkillImageUpdate();
            CoolTimeImageUpdate();
            BlockImageUpdate();
        }

        private void SkillImageUpdate()
        {
            if(skillImage == null) return;

            Sprite sprite = CurrentValue?.SkillSo?.SkillImage;
            
            skillImage.gameObject.SetActive(sprite != null);
            skillImage.sprite = sprite;
        }
        
        private void CoolTimeImageUpdate()
        {
            if(coolTimeImage == null) return;
            
            if (CurrentValue is IHasCoolTime coolTime)
            {
                coolTimeImage.gameObject.SetActive(true);
                coolTimeImage.fillAmount = coolTime.RemainingCoolTime / coolTime.CoolTime;
            }
            else
            {
                coolTimeImage.gameObject.SetActive(false);
                coolTimeImage.fillAmount = 0f;
            }
        }

        private void BlockImageUpdate()
        {
            if(blockImage == null) return;

            bool isSkillUse = CurrentValue?.CanUseSkill() ?? false;
            IHasCoolTime coolTime = CurrentValue as IHasCoolTime;
            bool isNotCoolTime = coolTime != null && Mathf.Approximately(coolTime.RemainingCoolTime,0f);
            blockImage.gameObject.SetActive(!isSkillUse && (coolTime == null || isNotCoolTime));
        }
        
    }
}