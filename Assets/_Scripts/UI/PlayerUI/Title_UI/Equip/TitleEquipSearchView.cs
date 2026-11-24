using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.UI.PlayerUI.Title_UI.Equip
{
    // 칭호 검색창. 입력이 바뀌면 ViewModel에 넘기고, ViewModel 값이 바뀌면(초기화 등) 알림 없이 화면만 맞춘다.
    public class TitleEquipSearchView : ViewBase<TitleEquipViewModel>
    {
        [SerializeField] private TMP_InputField searchInput;

        protected override void OnBind(TitleEquipViewModel vm)
        {
            UnityAction<string> onSearch = vm.SetSearch;
            searchInput.onValueChanged.AddListener(onSearch);
            Disposable.Create(() => searchInput.onValueChanged.RemoveListener(onSearch)).AddTo(Disposables);

            vm.Search.Subscribe(text => searchInput.SetTextWithoutNotify(text)).AddTo(Disposables);
        }
    }
}
