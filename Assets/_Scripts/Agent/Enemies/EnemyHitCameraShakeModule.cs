using _Scripts.DevLib.BattleSystem;
using _Scripts.DevLib.ModuleSystem;
using Unity.Cinemachine;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    // 실제 피격 결과를 짧은 Cinemachine Impulse로 전달합니다. 카메라 추적/Transform은 건드리지 않습니다.
    [DisallowMultipleComponent]
    public sealed class EnemyHitCameraShakeModule : Module
    {
        [Header("Normal Hit")]
        [Tooltip("흔들림 신호의 월드 유닛 배율. 카메라 Listener의 Gain이 최종 강도에 곱해집니다. 0이면 끕니다.")]
        [SerializeField, Min(0f)] private float _normalStrength = 0.06f;
        [SerializeField, Min(0.01f)] private float _normalDuration = 0.12f;

        [Header("Critical Hit")]
        [SerializeField, Min(0f)] private float _criticalStrength = 0.12f;
        [SerializeField, Min(0.01f)] private float _criticalDuration = 0.18f;

        [Header("Direction / Camera Range")]
        [SerializeField] private bool _useHitDirection = true;
        [Tooltip("피격 방향이 없거나 Use Hit Direction을 끈 경우 사용합니다. 길이는 강도에 영향을 주지 않습니다.")]
        [SerializeField] private Vector2 _fallbackDirection = new Vector2(1f, 0.3f);
        [Tooltip("현재 PlayerCam은 채널 1을 수신합니다. 보스 레이저의 채널 64와 별개입니다.")]
        [SerializeField, CinemachineImpulseChannelProperty] private int _impulseChannel = 1;
        [Tooltip("이 거리 안의 카메라는 전체 강도로 수신합니다. Use 2D Distance가 켜진 Listener를 사용합니다.")]
        [SerializeField, Min(0f)] private float _fullStrengthRadius = 12f;
        [Tooltip("전체 강도 범위 밖에서 흔들림이 0으로 줄어드는 추가 거리입니다.")]
        [SerializeField, Min(0.01f)] private float _fadeDistance = 8f;

        private EnemyBase _enemy;
        private bool _subscribed;

        public override void Initialize(ModuleOwner owner)
        {
            Unsubscribe();
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void Subscribe()
        {
            if (_subscribed || _enemy == null) return;
            _enemy.DamageReceived += OnDamageReceived;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && _enemy != null) _enemy.DamageReceived -= OnDamageReceived;
            _subscribed = false;
        }

        private void OnDamageReceived(DamageData damage, Vector2 point, Vector2 direction, Vector2 normal)
        {
            if (damage.DamageAmount <= 0f || !IsFinite(damage.DamageAmount)) return;
            if (!HasDirection(direction) && damage.Dealer != null)
                direction = _enemy.transform.position - damage.Dealer.transform.position;
            if (!HasDirection(direction)) direction = -normal;
            PlayShake(damage.IsCritical, direction);
        }

        // 연출만 실행합니다. 데미지 계산이나 크리티컬 확률은 변경하지 않습니다.
        public void PlayShake(bool isCritical, Vector2 direction)
        {
            if (!isActiveAndEnabled || _impulseChannel == 0) return;
            float strength = isCritical ? _criticalStrength : _normalStrength;
            float duration = isCritical ? _criticalDuration : _normalDuration;
            if (!IsFinite(strength) || strength <= 0f || !IsFinite(duration) || duration <= 0f) return;
            if (!_useHitDirection || !HasDirection(direction)) direction = _fallbackDirection;
            if (!HasDirection(direction)) direction = Vector2.right;

            // 재생 중인 신호는 Definition을 참조하므로, 타격마다 별도 설정을 사용합니다.
            // 다음 크리티컬/인스펙터 변경이 이미 재생 중인 일반 타격의 길이를 바꾸지 않습니다.
            var definition = new CinemachineImpulseDefinition
            {
                ImpulseChannel = _impulseChannel,
                ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
                ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Dissipating,
                ImpulseDuration = Mathf.Max(0.01f, duration),
                DissipationDistance = Mathf.Max(0.01f, _fadeDistance)
            };
            Vector3 position = _enemy != null ? _enemy.transform.position : transform.position;
            var impulse = definition.CreateAndReturnEvent(position, (Vector3)direction.normalized * strength);
            if (impulse == null) return;
            impulse.Radius = Mathf.Max(0f, _fullStrengthRadius);
            impulse.CustomDissipation = -1f;
            impulse.DissipationMode = CinemachineImpulseManager.ImpulseEvent.DissipationModes.SoftDecay;
            // Z 이동과 회전 없이 흔들며, 이미 생성된 신호는 적 사망 후에도 자연스럽게 끝납니다.
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool HasDirection(Vector2 value) => IsFinite(value.x) && IsFinite(value.y) && value.sqrMagnitude > 0.0001f;
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

#if UNITY_EDITOR
        [ContextMenu("Preview/Normal Shake (Play Mode, No Damage)")]
        private void PreviewNormal()
        {
            if (Application.isPlaying) PlayShake(false, _fallbackDirection);
        }

        [ContextMenu("Preview/Critical Shake (Play Mode, No Damage)")]
        private void PreviewCritical()
        {
            if (Application.isPlaying) PlayShake(true, _fallbackDirection);
        }
#endif
    }
}
