using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.SettingsUI
{
    // 좌우 화살표로 값을 순환하는 설정 행(SettingsRow_Cycle). 어떤 설정인지는 settingId로 고른다.
    public class SettingsCycleRowView : ViewBase<SettingsViewModel>
    {
        [SerializeField] private CycleSettingId settingId;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text valueText;

        protected override void OnBind(SettingsViewModel vm)
        {
            vm.GetCycleText(settingId).BindText(valueText).AddTo(Disposables);
            prevButton.BindClick(() => vm.StepCycle(settingId, -1)).AddTo(Disposables);
            nextButton.BindClick(() => vm.StepCycle(settingId, 1)).AddTo(Disposables);
        }
    }
}
