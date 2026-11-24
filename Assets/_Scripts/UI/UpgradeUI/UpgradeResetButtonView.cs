using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.UpgradeUI
{
    // 테크트리 초기화 버튼. 누르면 구매한 노드를 모두 되돌리고 쓴 재화를 돌려받는다.
    // ViewModel은 UpgradeTreeWindow가 SetViewModel로 넣는다.
    [RequireComponent(typeof(Button))]
    public class UpgradeResetButtonView : ViewBase<UpgradeTreeViewModel>
    {
        protected override void OnBind(UpgradeTreeViewModel vm)
        {
            GetComponent<Button>().BindClick(vm.ResetTree).AddTo(Disposables);
        }
    }
}
