using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Settings
{
    // 게임 설정 값을 들고, 바뀌면 적용·통지한다.
    // 각 시스템(사운드, HUD 등)은 Settings를 읽고 OnSettingsChanged를 구독해 자기 값을 반영한다.
    public interface ISettingsManager
    {
        event Action<SettingsData> OnSettingsChanged;

        SettingsData Settings { get; }

        // 이 모니터에서 고를 수 있는 해상도(작은 것부터).
        IReadOnlyList<Vector2Int> ResolutionOptions { get; }

        // 값을 보정해 적용하고, 실제로 바뀌었으면 OnSettingsChanged를 보낸다.
        void UpdateSettings(SettingsData data);

        void ResetToDefault();

        void Save();
    }
}
