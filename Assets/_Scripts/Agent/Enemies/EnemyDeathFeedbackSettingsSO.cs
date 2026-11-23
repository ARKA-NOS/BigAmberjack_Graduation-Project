using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    // 임시 사망 연출의 조정값을 보관합니다. 끄면 각 적과 이펙트의 기존 설정을 사용합니다.
    [CreateAssetMenu(fileName = "EnemyDeathFeedbackSettings", menuName = "Enemies/Death Feedback Settings")]
    public sealed class EnemyDeathFeedbackSettingsSO : ScriptableObject
    {
        [Tooltip("끄면 즉시 페이드와 아래의 이펙트·쉐이크 추가 배율을 모두 해제합니다.")]
        [SerializeField] private bool useTemporaryFeedback = true;

        [Header("Death Fade")]
        [SerializeField] private bool fadeImmediately = true;
        [SerializeField, Min(0.01f)] private float immediateFadeDuration = 0.1f;

        [Header("Death Burst")]
        [Tooltip("사망 혈흔과 금속 파편의 초기 속도에만 곱합니다. 피격 이펙트는 기존 설정을 사용합니다.")]
        [SerializeField, Min(0f)] private float burstSpeedMultiplier = 1.25f;
        [Tooltip("사망 버스트의 입자 수에 곱합니다. 이펙트 하나당 최대 128개로 제한합니다.")]
        [SerializeField, Range(1f, 4f)] private float burstCountMultiplier = 2f;

        [Header("Hit Camera Shake")]
        [SerializeField, Min(0f)] private float hitShakeStrengthMultiplier = 1.5f;
        [Tooltip("사망 타격에 추가로 곱합니다. 기본값에서 처치 쉐이크는 기존 대비 1.5×1.6=2.4배입니다.")]
        [SerializeField, Min(0f)] private float lethalShakeStrengthMultiplier = 1.6f;
        [SerializeField, Min(0.01f)] private float lethalShakeDurationMultiplier = 1.6f;

        public bool UseTemporaryFeedback => useTemporaryFeedback;
        public bool FadeImmediately => fadeImmediately;
        public float ImmediateFadeDuration => Mathf.Max(0.01f, immediateFadeDuration);
        public float BurstSpeedMultiplier => Mathf.Max(0f, burstSpeedMultiplier);
        public float BurstCountMultiplier => Mathf.Clamp(burstCountMultiplier, 1f, 4f);
        public float HitShakeStrengthMultiplier => Mathf.Max(0f, hitShakeStrengthMultiplier);
        public float LethalShakeStrengthMultiplier => Mathf.Max(0f, lethalShakeStrengthMultiplier);
        public float LethalShakeDurationMultiplier => Mathf.Max(0.01f, lethalShakeDurationMultiplier);
    }
}
