using System.Collections.Generic;
using _Scripts._Core._EventSystem;
using _Scripts._Core._Manager;
using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Managers
{
    // 전투 시간 제어의 공통 진입점입니다. 외부 UI/치트의 직접 쓰기도 이 서비스로 이관해야 합니다.
    [DefaultExecutionOrder(-15), DisallowMultipleComponent]
    public sealed class TimeStopService : AbstractManager
    {
        [SerializeField, Range(0f, 1f)] private float stopTimeScale = 0.05f;

        private sealed class SlowMotion
        {
            public object Owner;
            public float Multiplier, Hold, Recovery, Elapsed;
        }

        private readonly HashSet<object> _pauseOwners = new HashSet<object>();
        private readonly List<SlowMotion> _slowMotions = new List<SlowMotion>();
        private float _baseTimeScale = 1f;
        private float _endTime;
        private float _lastAppliedScale = 1f;
        private float _lastUnscaledTime;
        private bool _externalPause;
        private bool _subscribed;

        public bool IsStopped => Time.unscaledTime < _endTime;
        public bool IsPaused => _pauseOwners.Count > 0 || _externalPause;
        public float BaseTimeScale => _baseTimeScale;
        private bool IsRegistered => ServiceLocator.Get<TimeStopService>() == this;

        private void OnEnable() => Initialize();

        public override void Initialize()
        {
            if (!isActiveAndEnabled || IsRegistered) return;
            TimeStopService current = ServiceLocator.Get<TimeStopService>();
            if (current != null && current.isActiveAndEnabled)
            {
                Debug.LogWarning("TimeStopService는 하나만 활성화할 수 있습니다.", this);
                enabled = false;

                return;
            }

            _baseTimeScale = Time.timeScale;
            _lastAppliedScale = Time.timeScale;
            _lastUnscaledTime = Time.unscaledTime;
            ServiceLocator.Register(this);
            EventBus<HitStopEvent>.Subscribe(HandleHitStop);
            _subscribed = true;
        }

        private void HandleHitStop(HitStopEvent evt) => RequestHitStop(evt.Duration);

        public void RequestHitStop(float duration)
        {
            if (!IsRegistered || !Valid(duration) || duration <= 0f) return;

            // 중첩 요청은 먼저 들어온 종료 시간을 줄이지 않습니다.
            _endTime = Mathf.Max(_endTime, Time.unscaledTime + duration);
            RefreshScale();
        }

        public void RequestSlowMotion(object owner, float multiplier, float hold, float recovery)
        {
            if (!IsRegistered || owner == null || !Valid(multiplier) || !Valid(hold) || !Valid(recovery)) return;

            CancelSlowMotion(owner);
            _slowMotions.Add(new SlowMotion { Owner = owner, Multiplier = Mathf.Clamp(multiplier, 0.01f, 1f),
                Hold = Mathf.Max(0f, hold), Recovery = Mathf.Max(0f, recovery) });
            RefreshScale();
        }

        public void CancelSlowMotion(object owner)
        {
            if (!IsRegistered) return;

            for (int i = _slowMotions.Count - 1; i >= 0; i--)
                if (ReferenceEquals(_slowMotions[i].Owner, owner)) _slowMotions.RemoveAt(i);

            RefreshScale();
        }

        public void SetPaused(object owner, bool paused)
        {
            if (!IsRegistered || owner == null) return;

            if (paused) _pauseOwners.Add(owner);
            else _pauseOwners.Remove(owner);

            RefreshScale();
        }

        public void SetBaseTimeScale(float value)
        {
            if (!IsRegistered || !Valid(value) || value < 0f) return;

            _baseTimeScale = value;
            RefreshScale();
        }

        private void Update()
        {
            if (!IsRegistered) return;

            ObserveExternalPause();
            _pauseOwners.RemoveWhere(IsDestroyed);
            float delta = Mathf.Max(0f, Time.unscaledTime - _lastUnscaledTime);
            _lastUnscaledTime = Time.unscaledTime;

            // 기존 보스 사망 슬로우의 유지·복귀 시간은 일시정지 중에 소비하지 않습니다.
            if (!IsPaused && _baseTimeScale > 0f)
                for (int i = _slowMotions.Count - 1; i >= 0; i--)
                {
                    SlowMotion request = _slowMotions[i];
                    request.Elapsed += delta;
                    if (request.Elapsed >= request.Hold + request.Recovery) _slowMotions.RemoveAt(i);
                }

            RefreshScale();
        }

        private void RefreshScale()
        {
            if (!IsRegistered) return;

            ObserveExternalPause();
            // 팀의 기존 UI가 아직 직접 timeScale을 바꾸는 동안 그 일시정지를 덮지 않습니다.
            if (_externalPause) return;

            float scale = _baseTimeScale;
            foreach (SlowMotion request in _slowMotions)
            {
                float t = request.Recovery > 0f ? Mathf.Clamp01((request.Elapsed - request.Hold) / request.Recovery) :
                    request.Elapsed >= request.Hold ? 1f : 0f;
                float multiplier = Mathf.Lerp(request.Multiplier, 1f, Mathf.SmoothStep(0f, 1f, t));
                scale = Mathf.Min(scale, _baseTimeScale * multiplier);
            }

            if (IsStopped) scale = Mathf.Min(scale, stopTimeScale);
            if (_pauseOwners.Count > 0) scale = 0f;
            ApplyScale(scale);
        }

        private void ObserveExternalPause()
        {
            if (Time.timeScale <= 0f && _lastAppliedScale > 0f) _externalPause = true;
            else if (Time.timeScale > 0f) _externalPause = false;
        }

        private void ApplyScale(float scale)
        {
            Time.timeScale = scale;
            _lastAppliedScale = scale;
        }

        private static bool Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool IsDestroyed(object owner) => owner is Object unityObject && unityObject == null;

        public override void GameEnd() => Release();

        private void OnDisable() => Release();

        private void Release()
        {
            if (_subscribed) EventBus<HitStopEvent>.UnSubscribe(HandleHitStop);
            _subscribed = false;
            if (!IsRegistered) return;

            ObserveExternalPause();
            _endTime = 0f;
            _slowMotions.Clear();
            _pauseOwners.Clear();
            if (!_externalPause) ApplyScale(_baseTimeScale);
            ServiceLocator.Register<TimeStopService>(null);
        }
    }
}
