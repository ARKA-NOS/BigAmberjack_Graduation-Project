using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies.Managers;
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

        private EnemyBase _enemy;
        private TimeStopService _service;
        private bool _subscribed;
        private bool _played;

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

        private void PlaySlowMotion()
        {
            if (_played || !isActiveAndEnabled) return;

            _played = true;
            _service = ServiceLocator.Get<TimeStopService>();
            _service?.RequestSlowMotion(this, timeScaleMultiplier, holdDuration, recoveryDuration);
        }

        private void Unsubscribe()
        {
            if (_subscribed && _enemy != null) _enemy.DeathStarted -= PlaySlowMotion;
            _subscribed = false;
        }

        private void Release()
        {
            Unsubscribe();
            if (_service != null) _service.CancelSlowMotion(this);
            _service = null;
        }

        private void OnDisable() => Release();

        private void OnDestroy() => Release();
    }
}
