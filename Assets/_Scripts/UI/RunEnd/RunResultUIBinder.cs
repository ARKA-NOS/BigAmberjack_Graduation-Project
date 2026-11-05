using System.Collections.Generic;
using Lrw.Script.UI.BindSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SDW.Scripts.RunEnd
{
    // 탐험 종료 화면. RunResultViewModel에 Bind된 RunResultData를 표시한다.
    // 이 컴포넌트가 붙은 오브젝트는 항상 켜 두고, 화면 표시는 root로 켜고 끈다.
    // (꺼져 있으면 Awake가 실행되지 않아 ViewModel 구독이 빠진다)
    public class RunResultUIBinder : AbstractUIBinder<RunResultData>
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

        protected override void Awake()
        {
            base.Awake();
            nextButton.onClick.AddListener(HandleNextClicked);
            OnDataChanged();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            nextButton.onClick.RemoveListener(HandleNextClicked);
        }

        // 결과 내용은 Bind 시점에만 바뀌므로 매 프레임 갱신할 것이 없다.
        protected override void DataUpdate()
        {
        }

        protected override void OnDataChanged()
        {
            RunResultData data = CurrentValue;

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

        private void HandleNextClicked()
        {
            CurrentValue?.OnNext?.Invoke();
        }
    }
}
