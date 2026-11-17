using System;
using UnityEngine;

namespace _Scripts.Settings
{
    // 설정 창의 모든 값. SaveSystem(JsonUtility)으로 그대로 저장되는 단위라 public 필드로 둔다.
    // 값을 바꿀 때는 복사본의 필드를 고친 뒤 ISettingsManager.UpdateSettings에 넘긴다.
    [Serializable]
    public struct SettingsData : IEquatable<SettingsData>
    {
        public const int MaxPercent = 100;
        public static readonly int[] HudScaleOptions = { 90, 100, 110 };

        [Header("Graphics")]
        public int resolutionWidth;
        public int resolutionHeight;
        public ScreenModeOption screenMode;
        public bool lighting;
        [Range(0, MaxPercent)] public int screenShake;

        [Header("Audio")]
        [Range(0, MaxPercent)] public int masterVolume;
        [Range(0, MaxPercent)] public int musicVolume;
        [Range(0, MaxPercent)] public int effectsVolume;

        [Header("Gameplay")]
        public int hudScale;
        public bool damageNumbers;
        public TitleNoticeOption titleNotice;
        public bool runTimer;

        public Vector2Int Resolution => new(resolutionWidth, resolutionHeight);

        // 설정 시안의 기본값.
        public static SettingsData Default => new()
        {
            resolutionWidth = 1920,
            resolutionHeight = 1080,
            screenMode = ScreenModeOption.Borderless,
            lighting = true,
            screenShake = 100,
            masterVolume = 80,
            musicVolume = 65,
            effectsVolume = 85,
            hudScale = 100,
            damageNumbers = true,
            titleNotice = TitleNoticeOption.ProgressAndEarned,
            runTimer = false
        };

        // 저장 파일이 손상됐거나 예전 버전이어도 쓸 수 있는 값으로 맞춘다.
        public SettingsData Sanitized()
        {
            SettingsData defaults = Default;
            SettingsData result = this;

            if (result.resolutionWidth <= 0 || result.resolutionHeight <= 0)
            {
                result.resolutionWidth = defaults.resolutionWidth;
                result.resolutionHeight = defaults.resolutionHeight;
            }

            if (!Enum.IsDefined(typeof(ScreenModeOption), result.screenMode)) result.screenMode = defaults.screenMode;
            if (!Enum.IsDefined(typeof(TitleNoticeOption), result.titleNotice)) result.titleNotice = defaults.titleNotice;
            if (Array.IndexOf(HudScaleOptions, result.hudScale) < 0) result.hudScale = defaults.hudScale;

            result.screenShake = ClampPercent(result.screenShake);
            result.masterVolume = ClampPercent(result.masterVolume);
            result.musicVolume = ClampPercent(result.musicVolume);
            result.effectsVolume = ClampPercent(result.effectsVolume);
            return result;
        }

        private static int ClampPercent(int value) => Mathf.Clamp(value, 0, MaxPercent);

        public bool Equals(SettingsData other) =>
            resolutionWidth == other.resolutionWidth &&
            resolutionHeight == other.resolutionHeight &&
            screenMode == other.screenMode &&
            lighting == other.lighting &&
            screenShake == other.screenShake &&
            masterVolume == other.masterVolume &&
            musicVolume == other.musicVolume &&
            effectsVolume == other.effectsVolume &&
            hudScale == other.hudScale &&
            damageNumbers == other.damageNumbers &&
            titleNotice == other.titleNotice &&
            runTimer == other.runTimer;

        public override bool Equals(object obj) => obj is SettingsData other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(resolutionWidth);
            hash.Add(resolutionHeight);
            hash.Add(screenMode);
            hash.Add(lighting);
            hash.Add(screenShake);
            hash.Add(masterVolume);
            hash.Add(musicVolume);
            hash.Add(effectsVolume);
            hash.Add(hudScale);
            hash.Add(damageNumbers);
            hash.Add(titleNotice);
            hash.Add(runTimer);
            return hash.ToHashCode();
        }
    }
}
