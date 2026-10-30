using System;
using Lrw.Script.DataStructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.Title_UI
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

            checkImage.sprite = style.CheckSprite;
            nameText.text = viewData.QuestName;
            nameText.color = style.NameColor;
            fillImage.sprite = style.FillSprite;
            fillImage.fillAmount = gauge.Normalized;
            valueText.text = GetValueText(gauge);
            valueText.color = style.ValueColor;
        }

        private static string GetValueText(Gauge gauge)
            => $"{Mathf.FloorToInt(gauge.CurrentValue)}/{Mathf.FloorToInt(gauge.MaxValue)}";
    }
}
