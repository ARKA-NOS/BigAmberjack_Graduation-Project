using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.Title_UI
{
    public class TitleItemUI : MonoBehaviour
    {
        [Serializable]
        private struct TitleItemStyle
        {
            public Sprite RowSprite;
            public Sprite OpenRowSprite;
            public Sprite FillSprite;
            public Sprite CheckBoxSprite;
            public Color NameColor;
            public Color PercentColor;
        }

        [Header("Header")]
        [SerializeField] private Button headerButton;
        [SerializeField] private Image rowImage;
        [SerializeField] private Image arrowImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI percentText;
        [SerializeField] private Image checkBoxImage;

        [Header("Detail")]
        [SerializeField] private GameObject detail;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI effectText;
        [SerializeField] private QuestItemUI questItemPrefab;

        [Header("Style")]
        [SerializeField] private Sprite arrowDownSprite;
        [SerializeField] private Sprite arrowUpSprite;
        [SerializeField] private TitleItemStyle normalStyle;
        [SerializeField] private TitleItemStyle doneStyle;

        private List<QuestItemUI> _questItemUIs;
        private TitleViewData _viewData;

        public event Action<TitleItemUI> OnHeaderClick;

        public string TitleName => _viewData.TitleName;

        private void Awake()
        {
            _questItemUIs = new();
            headerButton.onClick.AddListener(HandleHeaderClick);
        }

        private void OnDestroy()
        {
            headerButton.onClick.RemoveListener(HandleHeaderClick);
        }

        public void SetViewData(TitleViewData viewData, bool isOpen)
        {
            _viewData = viewData;
            HeaderUpdate(isOpen);

            detail.SetActive(isOpen);
            if (isOpen)
                DetailUpdate();
        }

        private void HeaderUpdate(bool isOpen)
        {
            TitleItemStyle style = _viewData.IsCompleted ? doneStyle : normalStyle;
            float progress = _viewData.Progress;

            rowImage.sprite = isOpen ? style.OpenRowSprite : style.RowSprite;
            arrowImage.sprite = isOpen ? arrowUpSprite : arrowDownSprite;
            nameText.text = _viewData.TitleName;
            nameText.color = style.NameColor;
            fillImage.sprite = style.FillSprite;
            fillImage.fillAmount = progress;
            percentText.text = GetPercentText(progress);
            percentText.color = style.PercentColor;
            checkBoxImage.sprite = style.CheckBoxSprite;
        }

        private void DetailUpdate()
        {
            descriptionText.text = _viewData.TitleDescription;
            effectText.text = _viewData.UpgradeDescription;

            List<QuestViewData> quests = _viewData.TitleGauge;
            int questCount = quests?.Count ?? 0;

            SecuringQuestItemUI(questCount);

            for (int i = 0; i < _questItemUIs.Count; i++)
            {
                if (i < questCount)
                {
                    _questItemUIs[i].gameObject.SetActive(true);
                    _questItemUIs[i].SetViewData(quests[i]);
                }
                else
                {
                    _questItemUIs[i].gameObject.SetActive(false);
                }
            }
        }

        private void SecuringQuestItemUI(int count)
        {
            for (int i = _questItemUIs.Count; i < count; i++)
            {
                _questItemUIs.Add(Instantiate(questItemPrefab, detail.transform));
            }
        }

        private void HandleHeaderClick() => OnHeaderClick?.Invoke(this);

        private static string GetPercentText(float progress)
            => $"{Mathf.FloorToInt(progress * 100f)}%";
    }
}
