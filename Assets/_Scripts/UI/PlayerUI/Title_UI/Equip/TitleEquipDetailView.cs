using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerUI.Title_UI.Equip
{
    // 선택한 칭호의 상세(아이콘·이름·번호·달성 조건·효과)와 장착/해제 버튼을 그린다.
    // 해금하지 않은 칭호는 버튼이 눌리지 않고 "해금하지 않음"을 보여 준다. 비워 둔 칸은 건너뛴다.
    public class TitleEquipDetailView : ViewBase<TitleEquipViewModel>
    {
        [Tooltip("선택한 칭호가 없을 때 끌 내용 전체. 없으면 비워 둔다.")]
        [SerializeField] private GameObject content;

        [Header("Header")]
        [SerializeField] private Image iconImage;
        [Tooltip("칭호에 아이콘이 없을 때 보여 줄 아이콘(이전 런 기록 포함).")]
        [SerializeField] private Sprite defaultIcon;
        [Tooltip("미획득 칭호에 보여 줄 아이콘(잠금). 비워 두면 아이콘을 숨긴다.")]
        [SerializeField] private Sprite lockedIcon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI metaText;

        [Header("Body")]
        [SerializeField] private TextMeshProUGUI conditionText;
        [SerializeField] private TextMeshProUGUI effectText;
        [Tooltip("금기 줄. 미획득 칭호에서는 숨긴다. 없으면 비워 둔다.")]
        [SerializeField] private GameObject tabooRoot;

        [Header("Equip Button")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private TextMeshProUGUI toggleLabel;
        [SerializeField] private Graphic buttonGraphic;
        [SerializeField] private Color equipColor = new(0.941f, 0.722f, 0.251f);
        [SerializeField] private Color unequipColor = new(0.231f, 0.176f, 0.2f);
        [SerializeField] private Color disabledColor = new(0.18f, 0.137f, 0.157f);

        protected override void OnBind(TitleEquipViewModel vm)
        {
            vm.Detail.Subscribe(Show).AddTo(Disposables);
            toggleButton.BindClick(() => vm.Detail.Value?.OnToggle?.Invoke()).AddTo(Disposables);
        }

        private void Show(TitleEquipDetailData data)
        {
            if (content != null) content.SetActive(data != null);
            if (data == null) return;

            if (iconImage != null)
            {
                Sprite icon = data.State == TitleEquipState.Locked ? lockedIcon : data.Icon != null ? data.Icon : defaultIcon;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (nameText != null) nameText.text = data.Name;
            if (metaText != null) metaText.text = data.Meta;
            if (conditionText != null) conditionText.text = data.Condition;
            if (effectText != null) effectText.text = data.Effect;
            if (tabooRoot != null) tabooRoot.SetActive(data.State != TitleEquipState.Locked);

            toggleButton.interactable = data.CanToggle;
            if (toggleLabel != null) toggleLabel.text = data.ButtonLabel;
            if (buttonGraphic != null)
                buttonGraphic.color = !data.CanToggle ? disabledColor : data.IsEquipped ? unequipColor : equipColor;
        }
    }
}
