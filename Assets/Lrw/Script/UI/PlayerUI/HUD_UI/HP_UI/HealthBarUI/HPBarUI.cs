using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI.HealthBarUI
{
    [RequireComponent(typeof(LayoutElement), typeof(Image))]
    public class HPBarUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI healthText;
        
        private LayoutElement _healthLayout;

        private Image _healthImage;
        

        private void Awake()
        {
            _healthLayout = GetComponent<LayoutElement>();
            _healthImage = GetComponent<Image>();
        }

        public void SetBar(Color color, float flexible)
        {
            _healthImage.color = color;
            _healthLayout.flexibleWidth = flexible;
        }
        
        
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

    }
}