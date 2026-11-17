using System;
using System.Collections.Generic;
using _Scripts.Settings;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.SettingsUI
{
    // 설정 창 메인 화면의 값. 행마다 Observable 하나를 두고, View는 id로 자기 행 값을 가져간다.
    // 값을 바꿀 때는 Model의 현재 Settings를 복사해 필드 하나를 고친 뒤 UpdateSettings로 넘긴다.
    [CreateAssetMenu(fileName = "Settings View Model", menuName = "UI/MVVM/Settings View Model")]
    public class SettingsViewModel : ViewModelBase<ISettingsManager>
    {
        private const string ResetMessage = "초기값으로 설정되었습니다.";

        private static readonly ScreenModeOption[] ScreenModes =
            { ScreenModeOption.Fullscreen, ScreenModeOption.Borderless, ScreenModeOption.Windowed };
        private static readonly bool[] OnOff = { true, false };
        private static readonly bool[] OffOn = { false, true };
        private static readonly TitleNoticeOption[] TitleNotices =
            { TitleNoticeOption.ProgressAndEarned, TitleNoticeOption.EarnedOnly, TitleNoticeOption.Off };
        private static readonly Vector2Int[] FallbackResolutions = { SettingsData.Default.Resolution };

        private readonly Dictionary<CycleSettingId, Observable<string>> _cycleTexts = new();
        private readonly Dictionary<SliderSettingId, Observable<int>> _sliderValues = new();
        private readonly Observable<string> _statusMessage = new(string.Empty);

        private Dictionary<CycleSettingId, ISettingOptionCycle> _cycles;
        private Dictionary<SliderSettingId, SliderDefinition> _sliders;
        private ISettingsManager _model;

        public IReadOnlyObservable<string> StatusMessage => _statusMessage;

        #region Rows

        public IReadOnlyObservable<string> GetCycleText(CycleSettingId id) => GetOrCreate(_cycleTexts, id);

        public IReadOnlyObservable<int> GetSliderValue(SliderSettingId id) => GetOrCreate(_sliderValues, id);

        public void StepCycle(CycleSettingId id, int direction)
        {
            if (_model == null || !Cycles.TryGetValue(id, out ISettingOptionCycle cycle)) return;

            _statusMessage.Value = string.Empty;
            _model.UpdateSettings(cycle.Step(_model.Settings, direction));
        }

        public void SetSlider(SliderSettingId id, int value)
        {
            if (_model == null || !Sliders.TryGetValue(id, out SliderDefinition slider)) return;

            _statusMessage.Value = string.Empty;
            _model.UpdateSettings(slider.Set(_model.Settings, value));
        }

        #endregion

        public void ResetToDefault()
        {
            if (_model == null) return;

            _model.ResetToDefault();
            _statusMessage.Value = ResetMessage;
        }

        public void Save() => _model?.Save();

        protected override void OnConnect(ISettingsManager model)
        {
            _model = model;
            _model.OnSettingsChanged += Refresh;
            Refresh(_model.Settings);
        }

        protected override void OnDisconnect()
        {
            if (_model != null) _model.OnSettingsChanged -= Refresh;
            _model = null;
        }

        // Model이 없을 때는 기본값을 보여 준다.
        protected override void ResetState()
        {
            _statusMessage.Value = string.Empty;
            Refresh(SettingsData.Default);
        }

        private void Refresh(SettingsData data)
        {
            foreach (KeyValuePair<CycleSettingId, ISettingOptionCycle> pair in Cycles)
                GetOrCreate(_cycleTexts, pair.Key).Value = pair.Value.GetLabel(data);

            foreach (KeyValuePair<SliderSettingId, SliderDefinition> pair in Sliders)
                GetOrCreate(_sliderValues, pair.Key).Value = pair.Value.Get(data);
        }

        private static Observable<T> GetOrCreate<TKey, T>(Dictionary<TKey, Observable<T>> map, TKey key)
        {
            if (!map.TryGetValue(key, out Observable<T> observable))
            {
                observable = new Observable<T>();
                map.Add(key, observable);
            }

            return observable;
        }

        #region Definitions

        // ResetState가 OnEnable에서 먼저 불리므로 처음 쓸 때 만든다.
        private Dictionary<CycleSettingId, ISettingOptionCycle> Cycles => _cycles ??= CreateCycles();
        private Dictionary<SliderSettingId, SliderDefinition> Sliders => _sliders ??= CreateSliders();

        private Dictionary<CycleSettingId, ISettingOptionCycle> CreateCycles() => new()
        {
            [CycleSettingId.Resolution] = new SettingOptionCycle<Vector2Int>(
                () => _model?.ResolutionOptions ?? FallbackResolutions,
                r => $"{r.x} × {r.y}",
                d => d.Resolution,
                (d, r) => { d.resolutionWidth = r.x; d.resolutionHeight = r.y; return d; }),

            [CycleSettingId.ScreenMode] = new SettingOptionCycle<ScreenModeOption>(
                ScreenModes,
                m => m switch
                {
                    ScreenModeOption.Fullscreen => "전체 화면",
                    ScreenModeOption.Windowed => "창 모드",
                    _ => "테두리 없음"
                },
                d => d.screenMode,
                (d, m) => { d.screenMode = m; return d; }),

            [CycleSettingId.Lighting] = new SettingOptionCycle<bool>(
                OnOff, on => on ? "켜짐" : "꺼짐",
                d => d.lighting,
                (d, v) => { d.lighting = v; return d; }),

            [CycleSettingId.HudScale] = new SettingOptionCycle<int>(
                SettingsData.HudScaleOptions, s => $"{s}%",
                d => d.hudScale,
                (d, v) => { d.hudScale = v; return d; }),

            [CycleSettingId.DamageNumbers] = new SettingOptionCycle<bool>(
                OnOff, on => on ? "표시" : "숨기기",
                d => d.damageNumbers,
                (d, v) => { d.damageNumbers = v; return d; }),

            [CycleSettingId.TitleNotice] = new SettingOptionCycle<TitleNoticeOption>(
                TitleNotices,
                n => n switch
                {
                    TitleNoticeOption.EarnedOnly => "획득만",
                    TitleNoticeOption.Off => "끄기",
                    _ => "진행·획득"
                },
                d => d.titleNotice,
                (d, v) => { d.titleNotice = v; return d; }),

            [CycleSettingId.RunTimer] = new SettingOptionCycle<bool>(
                OffOn, on => on ? "표시" : "숨기기",
                d => d.runTimer,
                (d, v) => { d.runTimer = v; return d; })
        };

        private static Dictionary<SliderSettingId, SliderDefinition> CreateSliders() => new()
        {
            [SliderSettingId.ScreenShake] = new(d => d.screenShake, (d, v) => { d.screenShake = v; return d; }),
            [SliderSettingId.MasterVolume] = new(d => d.masterVolume, (d, v) => { d.masterVolume = v; return d; }),
            [SliderSettingId.MusicVolume] = new(d => d.musicVolume, (d, v) => { d.musicVolume = v; return d; }),
            [SliderSettingId.EffectsVolume] = new(d => d.effectsVolume, (d, v) => { d.effectsVolume = v; return d; })
        };

        private readonly struct SliderDefinition
        {
            public readonly Func<SettingsData, int> Get;
            public readonly Func<SettingsData, int, SettingsData> Set;

            public SliderDefinition(Func<SettingsData, int> get, Func<SettingsData, int, SettingsData> set)
            {
                Get = get;
                Set = set;
            }
        }

        #endregion
    }
}
