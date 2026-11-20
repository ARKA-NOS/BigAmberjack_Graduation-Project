using System;
using _Scripts.DataStructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    public class QuestItemUI : MonoBehaviour
    {
        [Serializable]
        private struct QuestItemStyle
        {
            public Sprite CheckSprite;
            public Sprite FillSprite;
            public Color NameColor;
            public Color ValueColor;
        }

        [SerializeField] private Image checkImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI valueText;

        [Header("Style")]
        [SerializeField] private QuestItemStyle normalStyle;
        [SerializeField] private QuestItemStyle doneStyle;

        public void SetViewData(QuestViewData viewData)
        {
            QuestItemStyle style = viewData.IsCompleted ? doneStyle : normalStyle;
            Gauge gauge = viewData.QuestGauge;

            // 체크 아이콘·채움 스프라이트는 비워 두면 건너뛴다.
            if (checkImage != null)
            {
                checkImage.sprite = style.CheckSprite;
                checkImage.enabled = style.CheckSprite != null;
            }
            nameText.text = viewData.QuestName;
            nameText.color = style.NameColor;
            if (style.FillSprite != null) fillImage.sprite = style.FillSprite;
            fillImage.fillAmount = gauge.Normalized;
            valueText.text = GetValueText(gauge);
            valueText.color = style.ValueColor;
        }

        private static string GetValueText(Gauge gauge)
            => $"{Mathf.FloorToInt(gauge.CurrentValue)} / {Mathf.FloorToInt(gauge.MaxValue)}";
    }
}
