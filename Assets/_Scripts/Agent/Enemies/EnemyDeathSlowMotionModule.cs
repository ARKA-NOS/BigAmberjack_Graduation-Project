using Cysharp.Threading.Tasks;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyDeathSlowMotionModule : Module
    {
        [SerializeField, Range(0.01f, 1f)] private float timeScaleMultiplier = 0.2f;
        [Tooltip("감속을 유지할 실시간입니다. 일시정지 중에는 진행하지 않습니다.")]
        [SerializeField, Min(0f)] private float holdDuration = 0.25f;
        [Tooltip("원래 시간 배율로 부드럽게 돌아갈 실시간입니다.")]
        [SerializeField, Min(0f)] private float recoveryDuration = 0.2f;
        private static EnemyDeathSlowMotionModule _active;
        private static bool _pendingRestore;
        private static float _pendingOriginal, _pendingApplied;

        private EnemyBase _enemy;
        private bool _subscribed;
        private bool _played;
        private bool _playing;
        private float _originalScale;
        private float _appliedScale;
        private float _elapsed;
        private float _lastUnscaledTime;

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
            _enemy.DeathStarted += PlaySlowMotion;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && _enemy != null) _enemy.DeathStarted -= PlaySlowMotion;
            _subscribed = false;
        }

        private void PlaySlowMotion()
        {
            if (_played || !isActiveAndEnabled) return;
            _played = true;
            RestorePendingIfResumed();
            if (Time.timeScale <= 0f) return;

            if (_active != null) _active.StopSlowMotion();
            _active = this;
            _originalScale = Time.timeScale;
            _appliedScale = _originalScale * Mathf.Clamp(timeScaleMultiplier, 0.01f, 1f);
            _elapsed = 0f;
            _lastUnscaledTime = Time.unscaledTime;
            _playing = true;
            Time.timeScale = _appliedScale;
        }

        private void Update()
        {
            if (!_playing) return;
            float delta = Time.unscaledTime - _lastUnscaledTime;
            _lastUnscaledTime = Time.unscaledTime;
            if (Time.timeScale <= 0f) return;
            // 다른 시스템이 바꾼 시간 배율은 덮어쓰지 않습니다.
            if (!Mathf.Approximately(Time.timeScale, _appliedScale)) { StopSlowMotion(); return; }

            _elapsed += delta;
            float recovery = Mathf.Max(0f, recoveryDuration);
            float t = recovery > 0f ? Mathf.Clamp01((_elapsed - Mathf.Max(0f, holdDuration)) / recovery) :
                _elapsed >= Mathf.Max(0f, holdDuration) ? 1f : 0f;
            _appliedScale = _originalScale * Mathf.Lerp(Mathf.Clamp(timeScaleMultiplier, 0.01f, 1f), 1f,
                Mathf.SmoothStep(0f, 1f, t));
            Time.timeScale = _appliedScale;
            if (t >= 1f) StopSlowMotion();
        }

        private void StopSlowMotion()
        {
            if (!_playing) return;
            _playing = false;
            if (_active == this) _active = null;

            if (Mathf.Approximately(Time.timeScale, _appliedScale)) Time.timeScale = _originalScale;
            else if (Time.timeScale <= 0f)
            {
                // 일시정지 UI가 기억한 감속 배율을 되돌린 다음 원래 배율을 복구합니다.
                _pendingOriginal = _originalScale;
                _pendingApplied = _appliedScale;
                _pendingRestore = true;
                RestoreAfterPauseAsync().Forget();
            }
        }

        private static async UniTaskVoid RestoreAfterPauseAsync()
        {
            while (_pendingRestore && Time.timeScale <= 0f)
                await UniTask.NextFrame(Application.exitCancellationToken);
            RestorePendingIfResumed();
        }

        private static void RestorePendingIfResumed()
        {
            if (!_pendingRestore || Time.timeScale <= 0f) return;
            if (Mathf.Approximately(Time.timeScale, _pendingApplied)) Time.timeScale = _pendingOriginal;
            _pendingRestore = false;
        }

        private void Release()
        {
            Unsubscribe();
            StopSlowMotion();
        }

        private void OnDisable() => Release();
        private void OnDestroy() => Release();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            _active = null;
            _pendingRestore = false;
        }
    }
}
