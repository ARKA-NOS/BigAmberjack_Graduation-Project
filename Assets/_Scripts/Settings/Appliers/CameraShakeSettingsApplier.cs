using _Scripts.Agent.Enemies;

namespace _Scripts.Settings.Appliers
{
    // 화면 흔들림 강도를 적 피격 카메라 쉐이크에 반영한다(100% = 원래 세기).
    public class CameraShakeSettingsApplier : ISettingsApplier
    {
        public void Apply(SettingsData data)
        {
            EnemyHitCameraShakeManager.SetIntensity(data.screenShake / (float)SettingsData.MaxPercent);
        }
    }
}
