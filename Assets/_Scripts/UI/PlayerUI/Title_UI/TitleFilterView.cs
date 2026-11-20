using System.Collections.Generic;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    // 칭호 검색창과 맵 선택. 입력이 바뀌면 ViewModel에 넘기고, ViewModel 값이 바뀌면(초기화 등) 알림 없이 화면만 맞춘다.
    public class TitleFilterView : ViewBase<TitleListViewModel>
    {
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Dropdown mapDropdown;

        protected override void OnBind(TitleListViewModel vm)
        {
            UnityAction<string> onSearch = vm.SetSearch;
            searchInput.onValueChanged.AddListener(onSearch);
            Disposable.Create(() => searchInput.onValueChanged.RemoveListener(onSearch)).AddTo(Disposables);

            UnityAction<int> onMap = vm.SetMap;
            mapDropdown.onValueChanged.AddListener(onMap);
            Disposable.Create(() => mapDropdown.onValueChanged.RemoveListener(onMap)).AddTo(Disposables);

            vm.Search.Subscribe(text => searchInput.SetTextWithoutNotify(text)).AddTo(Disposables);
            vm.MapOptions.Subscribe(ShowOptions).AddTo(Disposables);
            vm.MapIndex.Subscribe(index => mapDropdown.SetValueWithoutNotify(index)).AddTo(Disposables);
        }

        private void ShowOptions(IReadOnlyList<string> options)
        {
            int current = mapDropdown.value;
            mapDropdown.ClearOptions();
            mapDropdown.AddOptions(new List<string>(options));
            mapDropdown.SetValueWithoutNotify(Mathf.Clamp(current, 0, options.Count - 1));
            mapDropdown.RefreshShownValue();
        }
    }
}
