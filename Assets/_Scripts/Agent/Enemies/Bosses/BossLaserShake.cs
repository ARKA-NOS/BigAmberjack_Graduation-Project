using Unity.Cinemachine;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Bosses
{
    // CTJ 카메라의 External Impulse Listener로 전달합니다. 카메라 추적 코드는 수정하지 않습니다.
    [DisallowMultipleComponent]
    public sealed class BossLaserShake : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float amplitude = 0.8f;
        [SerializeField, Min(1f)] private float frequency = 32f;
        [Tooltip("횡단 끝에 남길 흔들림 비율입니다. 시작은 Amplitude 전체 강도를 사용합니다.")]
        [SerializeField, Range(0f, 1f)] private float endStrength = 0.15f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.18f;
        private CinemachineImpulseManager.ImpulseEvent _impulse;
        private LaserSignal _signal;
        private bool _fadingOut;
        private float _fadeStartedAt, _fadeFromStrength;
        public bool IsShaking => _impulse != null;
        public float Strength => _signal != null ? _signal.Strength : 0f;
        public const int ImpulseChannel = 64;

        public void StartShake(Bounds area)
        {
            StopShake();
            if (!isActiveAndEnabled || amplitude <= 0f) return;
            CinemachineImpulseManager manager = CinemachineImpulseManager.Instance;
            _signal = new LaserSignal(amplitude, frequency);
            _impulse = manager.NewImpulseEvent();
            _impulse.Position = area.center;
            _impulse.Channel = ImpulseChannel;
            _impulse.Radius = area.extents.magnitude + 4f;
            _impulse.DissipationDistance = 8f;
            _impulse.PropagationSpeed = 9999999f;
            _impulse.DirectionMode = CinemachineImpulseManager.ImpulseEvent.DirectionModes.Fixed;
            _impulse.Envelope = new CinemachineImpulseManager.EnvelopeDefinition { AttackTime = 0.02f, HoldForever = true };
            _impulse.SignalSource = _signal;
            manager.AddImpulseEvent(_impulse);
        }

        public void SetTraversalProgress(float progress)
        {
            if (_signal == null || _fadingOut) return;
            _signal.Strength = Mathf.Lerp(1f, Mathf.Clamp01(endStrength), Mathf.SmoothStep(0f, 1f, progress));
        }

        public void FadeOut()
        {
            if (_signal == null || _fadingOut) return;
            if (fadeOutDuration <= 0f) { StopShake(); return; }
            _fadingOut = true;
            _fadeStartedAt = Time.time;
            _fadeFromStrength = Strength;
        }

        private void Update()
        {
            if (!_fadingOut || _signal == null) return;
            float t = Mathf.Clamp01((Time.time - _fadeStartedAt) / fadeOutDuration);
            _signal.Strength = _fadeFromStrength * (1f - Mathf.SmoothStep(0f, 1f, t));
            if (t >= 1f) StopShake();
        }

        public void StopShake()
        {
            // 다른 효과의 이벤트나 이미 재사용된 이벤트를 취소하지 않습니다.
            if (_impulse != null && ReferenceEquals(_impulse.SignalSource, _signal))
            {
                _impulse.Cancel(CinemachineImpulseManager.Instance.CurrentTime, true);
                _impulse.SignalSource = null;
                // 같은 프레임에 취소하면 길이 0이 됩니다. Cinemachine은 양수 길이만 만료 처리하므로
                // 무음인 최소 수명을 남겨 이벤트 풀로 정상 회수되도록 합니다.
                if (_impulse.Envelope.Duration <= 0f) _impulse.Envelope.DecayTime = 0.001f;
            }
            _impulse = null;
            _signal = null;
            _fadingOut = false;
        }

        private void OnDisable() => StopShake();

        private sealed class LaserSignal : ISignalSource6D
        {
            private readonly float _amplitude, _frequency;
            public float Strength = 1f;
            public float SignalDuration => 0f;
            public LaserSignal(float amplitude, float frequency) { _amplitude = amplitude; _frequency = frequency; }
            public void GetSignal(float timeSinceSignalStart, out Vector3 pos, out Quaternion rot)
            {
                float t = timeSinceSignalStart * _frequency;
                pos = new Vector3(Mathf.PerlinNoise(t, 11.7f) * 2f - 1f, Mathf.PerlinNoise(37.2f, t) * 2f - 1f, 0f) * (_amplitude * Strength);
                rot = Quaternion.identity;
            }
        }
    }
}
