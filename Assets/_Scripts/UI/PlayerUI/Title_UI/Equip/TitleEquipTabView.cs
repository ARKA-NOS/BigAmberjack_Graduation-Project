using System;
using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerUI.Title_UI.Equip
{
    // 장착 창 필터 탭(전체 / 장착 중 / 보관 중 / 미획득). 탭마다 개수를 보여 주고, 선택된 탭은 색을 바꾼다.
    public class TitleEquipTabView : ViewBase<TitleEquipViewModel>
    {
        [Serializable]
        private class TabButton
        {
            public TitleEquipTab Tab;
            public Button Button;
            public TMP_Text Label;
            [Tooltip("개수를 보여 줄 글자. 없으면 비워 둔다.")] public TMP_Text Count;
            [Tooltip("선택된 탭에만 켜는 오브젝트. 없으면 비워 둔다.")] public GameObject Selected;
        }

        [SerializeField] private TabButton[] tabs = Array.Empty<TabButton>();
        [SerializeField] private Color selectedColor = new(0.945f, 0.827f, 0.549f);
        [SerializeField] private Color normalColor = new(0.949f, 0.902f, 0.886f);

        protected override void OnBind(TitleEquipViewModel vm)
        {
            foreach (TabButton tab in tabs)
            {
                TitleEquipTab target = tab.Tab;
                tab.Button.BindClick(() => vm.SetTab(target)).AddTo(Disposables);
            }

            vm.Tab.Subscribe(ShowSelected).AddTo(Disposables);
            vm.Counts.Subscribe(ShowCounts).AddTo(Disposables);
        }

        private void ShowSelected(TitleEquipTab selected)
        {
            foreach (TabButton tab in tabs)
            {
                bool on = tab.Tab == selected;
                if (tab.Label != null) tab.Label.color = on ? selectedColor : normalColor;
                if (tab.Count != null) tab.Count.color = on ? selectedColor : normalColor;
                if (tab.Selected != null) tab.Selected.SetActive(on);
            }
        }

        private void ShowCounts(TitleEquipTabCounts counts)
        {
            foreach (TabButton tab in tabs)
            {
                if (tab.Count != null) tab.Count.text = counts.Get(tab.Tab).ToString();
            }
        }
    }
}
