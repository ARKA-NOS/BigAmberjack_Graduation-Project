using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerUI.Title_UI.Equip
{
    // 장착 창 목록의 칭호 한 줄. 번호·이름·장착 중 표시를 보여 주고, 누르면 선택된다.
    // 상태에 따라 배경과 글자색이 달라진다(이번 런 달성 / 이전 런 기록 / 미획득). 비워 둔 칸은 건너뛴다.
    public class TitleEquipRowUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI numberText;
        [SerializeField] private TextMeshProUGUI nameText;
        [Tooltip("장착 중일 때만 켜는 표시. 없으면 비워 둔다.")]
        [SerializeField] private GameObject equippedMark;
        [Tooltip("미획득(???) 칭호일 때만 켜는 잠금 표시. 없으면 비워 둔다.")]
        [SerializeField] private GameObject lockedMark;
        [Tooltip("선택된 줄에만 켜는 테두리. 없으면 비워 둔다.")]
        [SerializeField] private GameObject selectedFrame;

        [Header("Colors")]
        [SerializeField] private Color ownedBackground = new(0.231f, 0.176f, 0.2f);
        [SerializeField] private Color recordedBackground = new(0.2f, 0.157f, 0.176f);
        [SerializeField] private Color lockedBackground = new(0.165f, 0.125f, 0.145f);
        [SerializeField] private Color selectedBackground = new(0.29f, 0.212f, 0.243f);
        [SerializeField] private Color ownedText = new(0.949f, 0.902f, 0.886f);
        [SerializeField] private Color recordedText = new(0.804f, 0.725f, 0.706f);
        [SerializeField] private Color lockedText = new(0.659f, 0.584f, 0.561f);

        private TitleEquipRowData _data;

        private void Awake()
        {
            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(HandleClick);
        }

        public void SetData(TitleEquipRowData data)
        {
            _data = data;

            if (numberText != null) numberText.text = string.IsNullOrEmpty(data.Number) ? string.Empty : $"No. {data.Number}";
            if (nameText != null)
            {
                nameText.text = data.Name;
                nameText.color = GetTextColor(data.State);
            }

            if (background != null)
                background.color = data.IsSelected ? selectedBackground : GetBackground(data.State);

            if (equippedMark != null) equippedMark.SetActive(data.IsEquipped);
            if (lockedMark != null) lockedMark.SetActive(data.State == TitleEquipState.Locked);
            if (selectedFrame != null) selectedFrame.SetActive(data.IsSelected);
        }

        private void HandleClick() => _data.OnSelect?.Invoke();

        private Color GetBackground(TitleEquipState state) => state switch
        {
            TitleEquipState.Recorded => recordedBackground,
            TitleEquipState.Locked => lockedBackground,
            _ => ownedBackground,
        };

        private Color GetTextColor(TitleEquipState state) => state switch
        {
            TitleEquipState.Recorded => recordedText,
            TitleEquipState.Locked => lockedText,
            _ => ownedText,
        };
    }
}
