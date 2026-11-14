using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.SettingsUI
{
    // 0~100 슬라이더 설정 행(SettingsRow_Slider). 어떤 설정인지는 settingId로 고른다.
    public class SettingsSliderRowView : ViewBase<SettingsViewModel>
    {
        [SerializeField] private SliderSettingId settingId;
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text percentText;

        protected override void OnBind(SettingsViewModel vm)
        {
            IReadOnlyObservable<int> value = vm.GetSliderValue(settingId);
            value.BindValue(slider, v => vm.SetSlider(settingId, v)).AddTo(Disposables);
            value.BindText(percentText, v => $"{v}%").AddTo(Disposables);
        }
    }
}
