using _Scripts.DevLib.BattleSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.UI.HitFeedback;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyDamageIndicatorModule : Module
    {
        [SerializeField] private DamageIndicatorViewModel _viewModel;
        [SerializeField] private Vector2 _worldOffset = new Vector2(0f, 0.45f);
        [Tooltip("유효한 피격 좌표 대신 적의 중심에서 표시하고 싶으면 끕니다.")]
        [SerializeField] private bool _useHitPoint = true;
        [SerializeField, Min(0f)] private float _maxHitPointDistance = 4f;

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
            Vector3 origin = _enemy.transform.position;
            Vector3 position = origin;
            if (_useHitPoint && IsFinite(point.x) && IsFinite(point.y) &&
                Vector2.Distance(point, origin) <= _maxHitPointDistance)
                position = new Vector3(point.x, point.y, origin.z);
            ShowDamage(damage.DamageAmount, damage.IsCritical, position + (Vector3)_worldOffset);
        }

        // 표시만 요청합니다. HP 변경이나 크리티컬 확률/배율 계산은 하지 않습니다.
        public void ShowDamage(float damage, bool isCritical, Vector3 position)
        {
            if (!isActiveAndEnabled || _viewModel == null || !IsFinite(damage) || damage <= 0f ||
                !IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z)) return;
            _viewModel.Bind(new DamageIndicatorData(GetInstanceID(), damage, isCritical, position));
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

#if UNITY_EDITOR
        [ContextMenu("Preview/Normal 24 (Play Mode, No Damage)")]
        private void PreviewNormal()
        {
            if (Application.isPlaying) ShowDamage(24f, false, transform.position + (Vector3)_worldOffset);
        }

        [ContextMenu("Preview/Critical 24 (Play Mode, No Damage)")]
        private void PreviewCritical()
        {
            if (Application.isPlaying) ShowDamage(24f, true, transform.position + (Vector3)_worldOffset);
        }
#endif
    }
}
