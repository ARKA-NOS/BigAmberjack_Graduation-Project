using System.Globalization;
using Lrw.Script.DataStructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.Effect_UI_System.Effect_UI
{
    public class PlayerEffectUI : MonoBehaviour
    {
        [SerializeField] private Image effectImage;
        [SerializeField] private Image normalTimeImage;
        [SerializeField] private TextMeshProUGUI effectTimeText;
        
        private EffectViewData effectData;

        private void Awake()
        {
            UpdateUI();
        }

        public void SetViewData(EffectViewData viewData)
        {
            effectData = viewData;
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            effectImage.sprite = effectData.EffectIcon;
            
            Gauge gauge = effectData.EffectGauge;
            normalTimeImage.fillAmount = gauge.Normalized;
            effectTimeText.text = GetRemainingTime(gauge.CurrentValue);
        }

        private string GetRemainingTime(float gaugeCurrentValue)
        {
            float remainingTime = Mathf.Max(0f, gaugeCurrentValue);
            return remainingTime.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}