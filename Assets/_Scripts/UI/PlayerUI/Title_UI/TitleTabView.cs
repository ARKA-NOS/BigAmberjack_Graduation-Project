using System;
using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    // 도감 탭 버튼(이번 런 / 달성한 칭호 / 전체 칭호). 선택된 탭은 글자색을 바꾸고 밑줄을 켠다.
    public class TitleTabView : ViewBase<TitleListViewModel>
    {
        [Serializable]
        private class TabButton
        {
            public TitleTab Tab;
            public Button Button;
            public TMP_Text Label;
            public GameObject Selected;
        }

        [SerializeField] private TabButton[] tabs = Array.Empty<TabButton>();
        [SerializeField] private Color selectedColor = new(0.945f, 0.827f, 0.549f);
        [SerializeField] private Color normalColor = new(0.722f, 0.788f, 0.847f);

        protected override void OnBind(TitleListViewModel vm)
        {
            foreach (TabButton tab in tabs)
            {
                TitleTab target = tab.Tab;
                tab.Button.BindClick(() => vm.SetTab(target)).AddTo(Disposables);
            }

            vm.Tab.Subscribe(ShowSelected).AddTo(Disposables);
        }

        private void ShowSelected(TitleTab selected)
        {
            foreach (TabButton tab in tabs)
            {
                bool on = tab.Tab == selected;
                if (tab.Label != null) tab.Label.color = on ? selectedColor : normalColor;
                if (tab.Selected != null) tab.Selected.SetActive(on);
            }
        }
    }
}
