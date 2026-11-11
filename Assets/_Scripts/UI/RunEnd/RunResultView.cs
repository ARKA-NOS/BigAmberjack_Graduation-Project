using System.Collections.Generic;
using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.RunEnd
{
    // 탐험 종료 결과창. RunResultViewModel.Result를 그리고, "다음" 버튼을 결과 데이터의 OnNext에 넘긴다.
    // 이 오브젝트는 항상 켜 두고 화면 표시는 root로 켜고 끈다(View 자신을 끄면 구독이 끊겨 다시 열리지 않는다).
    public class RunResultView : ViewBase<RunResultViewModel>
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image goldIcon;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text killCountText;
        [Tooltip("아이템 칸 배경 이미지. 아이템이 있는 칸만 보인다. 인벤토리 칸 순서대로 넣는다.")]
        [SerializeField] private List<Image> itemSlotBackgrounds = new();
        [Tooltip("아이템 칸의 아이콘 이미지. 인벤토리 칸 순서대로 넣는다.")]
        [SerializeField] private List<Image> itemSlotIcons = new();
        [SerializeField] private Button nextButton;

        protected override void OnBind(RunResultViewModel vm)
        {
            vm.Result.Subscribe(Show).AddTo(Disposables);
            nextButton.BindClick(() => vm.Result.Value?.OnNext?.Invoke()).AddTo(Disposables);
        }

        private void Show(RunResultData data)
        {
            if (data == null)
            {
                root.SetActive(false);
                return;
            }

            goldIcon.sprite = data.GoldIcon;
            goldIcon.enabled = data.GoldIcon != null;
            goldText.text = $"{data.Gold} G";

            killCountText.text = data.KillCount.HasValue ? data.KillCount.Value.ToString() : "-";

            for (int i = 0; i < itemSlotIcons.Count; i++)
            {
                Sprite icon = i < data.ItemIcons.Count ? data.ItemIcons[i] : null;
                bool hasItem = icon != null;

                itemSlotIcons[i].sprite = icon;
                itemSlotIcons[i].enabled = hasItem;

                // 칸 배경은 끄기만 하고 오브젝트는 남겨서, 아이템 위치(칸 순서)가 밀리지 않게 한다.
                if (i < itemSlotBackgrounds.Count)
                    itemSlotBackgrounds[i].enabled = hasItem;
            }

            root.SetActive(true);
        }
    }
}
