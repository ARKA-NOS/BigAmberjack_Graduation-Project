using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.Enemy
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas))]
    public sealed class BossHealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private Image border;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text invulnerableLabel;
        [SerializeField] private Color normalBorderColor = new Color(0.25f, 0.28f, 0.32f, 1f);
        private BossBase _boss;
        private INewHealthModule _health;
        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.enabled = false;
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        public void Bind(BossBase boss)
        {
            Unsubscribe();
            _boss = boss;
            if (isActiveAndEnabled) Subscribe();
        }

        private void Subscribe()
        {
            Unsubscribe();
            if (_boss != null) _health = _boss.GetModule<INewHealthModule>();
            if (_health != null)
            {
                _health.OnHealthChanged += OnHealthChanged;
                OnHealthChanged(_health.CurrentHealth, 0f, _health.MaxHealth);
            }
            Refresh();
        }

        private void OnHealthChanged(float current, float delta, float max)
        {
            if (healthFill != null) healthFill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (current <= 0f && _canvas != null) _canvas.enabled = false;
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            bool visible = _boss != null && _boss.isActiveAndEnabled && !_boss.IsDead &&
                _health != null && !_health.IsDead && _health.CurrentHealth > 0f;
            _canvas.enabled = visible;
            if (!visible) return;
            if (nameLabel != null && nameLabel.text != _boss.DisplayName) nameLabel.text = _boss.DisplayName;
            // 무적은 HP 변화 없이도 바뀌므로 보스의 실제 상태를 별도로 읽습니다.
            bool invulnerable = _boss.IsInvulnerable;
            if (border != null) border.color = invulnerable ? Color.white : normalBorderColor;
            if (invulnerableLabel != null) invulnerableLabel.gameObject.SetActive(invulnerable);
        }

        private void Unsubscribe()
        {
            if (_health != null) _health.OnHealthChanged -= OnHealthChanged;
            _health = null;
        }

        private void OnEnable()
        {
            if (_boss != null) Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (_canvas != null) _canvas.enabled = false;
        }

        private void OnDestroy() => Unsubscribe();
    }
}
