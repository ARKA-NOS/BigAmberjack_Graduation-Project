using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI.New_HP_UI.HealthBarUI
{
    [RequireComponent(typeof(LayoutElement), typeof(Image))]
    public class HPBarUI : MonoBehaviour
    {
        private LayoutElement healthLayout;

        private Image healthImage;

        private RectTransform _rectTransform;

        private void Awake()
        {
            healthLayout = GetComponent<LayoutElement>();
            healthImage = GetComponent<Image>();
            _rectTransform = transform as RectTransform;
        }

        public void SetBar(Color color, float flexible)
        {
            healthImage.color = color;
            healthLayout.flexibleWidth = flexible;
        }

        public void SetScale(float scale)
        {
            _rectTransform.localScale = new Vector3(scale, 1, 1);
        }
        
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

    }
}