using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.SettingsUI
{
    // 데이터 구역: 설정 기본값 복원 버튼과 결과 문구.
    public class SettingsDataView : ViewBase<SettingsViewModel>
    {
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text statusText;

        protected override void OnBind(SettingsViewModel vm)
        {
            resetButton.BindClick(vm.ResetToDefault).AddTo(Disposables);
            vm.StatusMessage.BindText(statusText).AddTo(Disposables);
        }
    }
}
