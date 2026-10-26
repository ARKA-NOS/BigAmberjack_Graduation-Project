using Lrw.Script.DataStructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI.New_HP_UI.HealthBarUI
{
    [RequireComponent(typeof(LayoutElement), typeof(Image))]
    public class HPBarUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI healthText;
        
        private LayoutElement _healthLayout;

        private Image _healthImage;
        
        private const string HealthFormat = "+{0}";
        private const string GaugeHealthFormat = "{0} / {1}";

        private void Awake()
        {
            _healthLayout = GetComponent<LayoutElement>();
            _healthImage = GetComponent<Image>();
        }

        public void SetBar(Color color, Color textColor, float flexible, Gauge gauge , bool isMaxHealth)
        {
            _healthImage.color = color;
            _healthLayout.flexibleWidth = flexible;
            
            healthText.color = textColor;
            healthText.text = GetHealthText(gauge, isMaxHealth);
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