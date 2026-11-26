using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.UpgradeUI
{
    // 임시 선택한 노드를 확정(구매)하는 버튼. 고른 것이 없으면 눌리지 않는다.
    // ViewModel은 UpgradeTreeWindow가 SetViewModel로 넣는다.
    [RequireComponent(typeof(Button))]
    public class UpgradeConfirmButtonView : ViewBase<UpgradeTreeViewModel>
    {
        protected override void OnBind(UpgradeTreeViewModel vm)
        {
            Button button = GetComponent<Button>();
            button.BindClick(vm.Confirm).AddTo(Disposables);
            vm.HasPending.Subscribe(hasPending => button.interactable = hasPending).AddTo(Disposables);
        }
    }
}
