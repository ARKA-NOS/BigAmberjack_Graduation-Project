using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    // 칭호 한 줄. 헤더에는 아이콘·이름·조건·번호·상태·퀘스트 진행을 항상 보여 주고, 헤더를 누르면 달성 효과(Detail)를 펼친다.
    // 잠긴 칭호(달성 기록 없음)는 이름을 ???로, 아이콘 자리에 ?를 보여 주고 펼칠 수 없다.
    // 비워 둔 칸(전체 진행 바, 퍼센트 등)은 건너뛴다.
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
        [SerializeField] private Image iconImage;
        [Tooltip("칭호에 아이콘이 없을 때 보여 줄 아이콘. 비워 두면 아이콘을 숨긴다.")]
        [SerializeField] private Sprite defaultIcon;
        [Tooltip("잠긴 칭호의 아이콘 자리에 보여 줄 표시(?). 없으면 비워 둔다.")]
        [SerializeField] private GameObject lockedMark;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [Tooltip("\"No. 001 · 1번 맵\" 줄. 없으면 비워 둔다.")]
        [SerializeField] private TextMeshProUGUI metaText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI percentText;
        [SerializeField] private Image checkBoxImage;
        [Tooltip("퀘스트 진행 줄을 놓을 곳. 비워 두면 Detail 아래에 놓는다.")]
        [SerializeField] private Transform questParent;

        [Header("Detail")]
        [SerializeField] private GameObject detail;
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

        // 펼침 상태를 기억할 때 쓰는 칭호 구분 값.
        public string Key => _viewData.Key;

        private void Awake()
        {
            _questItemUIs = new();
            // 직렬화 필드는 비어 있어도 가짜 null일 수 있어서 ??= 대신 == null로 확인한다.
            if (questParent == null) questParent = detail.transform;
            headerButton.onClick.AddListener(HandleHeaderClick);
        }

        private void OnDestroy()
        {
            headerButton.onClick.RemoveListener(HandleHeaderClick);
        }

        public void SetViewData(TitleViewData viewData, bool isOpen)
        {
            _viewData = viewData;
            isOpen &= !viewData.IsLocked;

            HeaderUpdate(isOpen);
            QuestUpdate();

            detail.SetActive(isOpen);
            if (isOpen)
                DetailUpdate();
        }

        private void HeaderUpdate(bool isOpen)
        {
            TitleItemStyle style = _viewData.IsCompleted ? doneStyle : normalStyle;
            bool locked = _viewData.IsLocked;
            float progress = _viewData.Progress;

            headerButton.interactable = !locked;

            if (rowImage != null)
            {
                Sprite rowSprite = isOpen ? style.OpenRowSprite : style.RowSprite;
                if (rowSprite != null) rowImage.sprite = rowSprite;
            }

            if (arrowImage != null)
            {
                arrowImage.sprite = isOpen ? arrowUpSprite : arrowDownSprite;
                arrowImage.enabled = !locked;
            }

            if (iconImage != null)
            {
                Sprite icon = locked ? null : _viewData.Icon != null ? _viewData.Icon : defaultIcon;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (lockedMark != null)
                lockedMark.SetActive(locked);

            nameText.text = _viewData.TitleName;
            nameText.color = style.NameColor;

            if (descriptionText != null)
            {
                descriptionText.text = _viewData.TitleDescription;
                descriptionText.gameObject.SetActive(!locked);
            }

            if (metaText != null)
                metaText.text = GetMetaText(_viewData);

            if (statusText != null)
            {
                statusText.text = GetStatusText(_viewData.Status);
                statusText.color = style.PercentColor;
            }

            if (fillImage != null)
            {
                if (style.FillSprite != null) fillImage.sprite = style.FillSprite;
                fillImage.fillAmount = progress;
            }

            if (percentText != null)
            {
                percentText.text = GetPercentText(progress);
                percentText.color = style.PercentColor;
            }

            if (checkBoxImage != null)
            {
                checkBoxImage.sprite = style.CheckBoxSprite;
                checkBoxImage.enabled = style.CheckBoxSprite != null;
            }
        }

        private void DetailUpdate()
        {
            effectText.text = _viewData.UpgradeDescription;
        }

        private void QuestUpdate()
        {
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
                _questItemUIs.Add(Instantiate(questItemPrefab, questParent));
            }
        }

        private void HandleHeaderClick() => OnHeaderClick?.Invoke(this);

        // 잠긴 칭호는 맵도 숨기고 번호만 보여 준다.
        private static string GetMetaText(TitleViewData data)
        {
            string number = data.Number > 0 ? $"No. {data.Number:000}" : string.Empty;
            if (data.IsLocked || string.IsNullOrEmpty(data.MapLabel)) return number;
            return number.Length > 0 ? $"{number} · {data.MapLabel}" : data.MapLabel;
        }

        private static string GetStatusText(TitleEntryStatus status) => status switch
        {
            TitleEntryStatus.RunCompleted => "이번 런 달성",
            TitleEntryStatus.Earned => "달성 기록 있음",
            TitleEntryStatus.NotEarned => "미달성",
            _ => "이번 런 진행 중",
        };

        private static string GetPercentText(float progress)
            => $"{Mathf.FloorToInt(progress * 100f)}%";
    }
}
