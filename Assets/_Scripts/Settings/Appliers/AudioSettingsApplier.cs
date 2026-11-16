using UnityEngine;

namespace _Scripts.Settings.Appliers
{
    // 전체 음량을 반영한다.
    // 배경 음악·효과음은 AudioMixer(그룹별 노출 파라미터)가 생기면 여기서 mixer.SetFloat로 함께 반영한다.
    public class AudioSettingsApplier : ISettingsApplier
    {
        public void Apply(SettingsData data)
        {
            AudioListener.volume = data.masterVolume / (float)SettingsData.MaxPercent;
        }
    }
}
