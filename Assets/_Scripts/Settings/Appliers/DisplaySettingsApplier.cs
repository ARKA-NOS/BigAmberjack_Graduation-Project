using _Scripts._Core._Debug;
using UnityEngine;

namespace _Scripts.Settings.Appliers
{
    // 해상도와 화면 모드를 반영한다. 에디터에서는 Screen.SetResolution이 Game 뷰에 적용되지 않는다.
    public class DisplaySettingsApplier : ISettingsApplier
    {
        private bool _hasApplied;
        private Vector2Int _appliedResolution;
        private FullScreenMode _appliedMode;

        public void Apply(SettingsData data)
        {
            Vector2Int resolution = data.Resolution;
            FullScreenMode mode = ToFullScreenMode(data.screenMode);

            if (_hasApplied && resolution == _appliedResolution && mode == _appliedMode) return;

            _hasApplied = true;
            _appliedResolution = resolution;
            _appliedMode = mode;

            // 이미 그 상태면(다시 생성된 매니저가 처음 적용할 때 등) 창을 다시 만들지 않는다.
            if (Screen.width == resolution.x && Screen.height == resolution.y && Screen.fullScreenMode == mode) return;

            Screen.SetResolution(resolution.x, resolution.y, mode);
            FDebug.Log($"[DisplaySettingsApplier] {resolution.x}x{resolution.y} {mode}");
        }

        private static FullScreenMode ToFullScreenMode(ScreenModeOption option) => option switch
        {
            ScreenModeOption.Fullscreen => FullScreenMode.ExclusiveFullScreen,
            ScreenModeOption.Windowed => FullScreenMode.Windowed,
            _ => FullScreenMode.FullScreenWindow
        };
    }
}
