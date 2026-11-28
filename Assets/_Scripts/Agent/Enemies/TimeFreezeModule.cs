using _Scripts._Core._EventSystem;
using _Scripts.Agent.Enemies.Managers;
using _Scripts.Agent.Players;
using _Scripts.DevLib.BattleSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    [DisallowMultipleComponent]
    public sealed class TimeFreezeModule : Module
    {
        [Tooltip("플레이어의 공격으로 실제 HP가 줄었을 때의 정지 시간입니다. 실시간 기준입니다.")]
        [SerializeField, Min(0f)] private float freezeDuration = 0.05f;

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

        private void OnDamageReceived(DamageData damage, Vector2 point, Vector2 direction, Vector2 normal)
        {
            if (damage.DamageAmount <= 0f || damage.Dealer is not PlayerController || freezeDuration <= 0f) return;

            EventBus<HitStopEvent>.Raise(new HitStopEvent(freezeDuration));
        }

        private void Unsubscribe()
        {
            if (_subscribed && _enemy != null) _enemy.DamageReceived -= OnDamageReceived;
            _subscribed = false;
        }

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();
    }
}
