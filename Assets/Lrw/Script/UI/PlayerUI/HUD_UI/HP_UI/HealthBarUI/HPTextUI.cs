using Lrw.Script.DataStructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI.HealthBarUI
{
    [RequireComponent(typeof(LayoutElement),typeof(TextMeshProUGUI))]
    public class HPTextUI : MonoBehaviour
    {
        private TextMeshProUGUI _textMeshProUGUI;
        
        private LayoutElement _layoutElement;
        
        private const string HealthFormat = "+{0}";
        private const string GaugeHealthFormat = "{0} / {1}";
        
        private void Awake()
        {
            _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            _layoutElement = GetComponent<LayoutElement>();
        }

        public void SetText(Color color, Gauge gauge, bool useMaxHealth)
        {
            string text = GetHealthText(gauge, useMaxHealth);
            _layoutElement.flexibleWidth = text.Length;
            _textMeshProUGUI.color = color;
            _textMeshProUGUI.text = text;
        }

        private string GetHealthText(Gauge gauge, bool useMaxHealth)
            => useMaxHealth ? 
                string.Format(GaugeHealthFormat, gauge.CurrentValue, gauge.MaxValue) : 
                string.Format(HealthFormat, gauge.CurrentValue);
        
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        
    }
}