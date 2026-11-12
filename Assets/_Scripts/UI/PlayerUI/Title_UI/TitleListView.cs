using System.Collections.Generic;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    // 칭호 목록을 그린다. 칭호마다 TitleItemUI를 이 오브젝트 아래에 만들고, 헤더를 누르면 상세를 펼치거나 접는다.
    public class TitleListView : ViewBase<TitleListViewModel>
    {
        [SerializeField] private TitleItemUI titleItemPrefab;

        private readonly List<TitleItemUI> _titleItemUIs = new();
        // 목록이 진행도순으로 다시 정렬되므로 펼침 상태는 줄 순서가 아니라 칭호 이름으로 기억한다.
        private readonly HashSet<string> _openTitles = new();

        protected override void OnBind(TitleListViewModel vm)
        {
            if (titleItemPrefab == null)
            {
                Debug.LogError($"[{name}] titleItemPrefab이 없습니다.", this);
                return;
            }

            vm.Titles.Subscribe(_ => Redraw()).AddTo(Disposables);
        }

        private void Redraw()
        {
            IReadOnlyList<TitleViewData> list = ViewModel.Titles;

            SecuringTitleItemUI(list.Count);

            for (int i = 0; i < _titleItemUIs.Count; i++)
            {
                bool shown = i < list.Count;
                _titleItemUIs[i].gameObject.SetActive(shown);
                if (shown)
                    _titleItemUIs[i].SetViewData(list[i], _openTitles.Contains(list[i].TitleName));
            }
        }

        private void SecuringTitleItemUI(int count)
        {
            for (int i = _titleItemUIs.Count; i < count; i++)
            {
                TitleItemUI titleItemUI = Instantiate(titleItemPrefab, transform);
                titleItemUI.OnHeaderClick += ToggleOpen;
                _titleItemUIs.Add(titleItemUI);
            }
        }

        private void ToggleOpen(TitleItemUI titleItemUI)
        {
            if (!_openTitles.Remove(titleItemUI.TitleName))
                _openTitles.Add(titleItemUI.TitleName);

            if (ViewModel != null)
                Redraw();
        }
    }
}
