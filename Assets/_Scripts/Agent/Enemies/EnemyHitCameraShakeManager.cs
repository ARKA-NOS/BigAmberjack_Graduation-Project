using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    // 게임 옵션에서 적 피격 카메라 쉐이크의 전체 강도를 조절하는 진입점입니다.
    public static class EnemyHitCameraShakeManager
    {
        public static float Intensity { get; private set; } = 1f;

        public static void SetIntensity(float intensity)
        {
            if (float.IsNaN(intensity) || float.IsInfinity(intensity)) return;
            Intensity = Mathf.Clamp01(intensity);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Intensity = 1f;
    }
}
