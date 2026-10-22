using Lrw.Script.DataStructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI
{
    /// <summary>
    /// 체력 하나를 표시하는 UI. HealthLayerUIBinder가 체력 수만큼 만든다.
    /// </summary>
    public class HealthLayerUI : MonoBehaviour
    {
        [SerializeField] private Image bar;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI valueText;

        public void SetViewData(HealthViewData viewData)
        {
            Gauge gauge = viewData.Gauge;
            float normal = gauge.MaxValue > 0f ? gauge.Normalized : 0f;

            if (bar != null)
            {
                bar.color = viewData.Data.HealthColor;
                bar.rectTransform.localScale = new Vector3(normal, 1, 1);
            }

            if (icon != null)
            {
                icon.sprite = viewData.Data.Icon;
                icon.enabled = viewData.Data.Icon != null;
            }

            if (valueText != null)
                valueText.text = $"{gauge.CurrentValue:0} / {gauge.MaxValue:0}";
        }
    }
}
