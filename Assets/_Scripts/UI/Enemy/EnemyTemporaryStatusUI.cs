using _Scripts.Agent.Enemies;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.Enemy
{
    // 임시 표시 전용. 적/모듈에서 이 UI를 참조하지 않으므로 자식 오브젝트째 삭제해도 전투는 유지됩니다.
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas), typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class EnemyTemporaryStatusUI : MonoBehaviour
    {
        [SerializeField] private Image healthFill;

        [Tooltip("적 루트에서 UI까지의 월드 좌표 오프셋. 상하 반전 시 Y만 반전하며 바 자체는 뒤집지 않습니다.")]
        [SerializeField] private Vector2 worldOffset = new Vector2(0f, 1.5f);
        [Tooltip("Canvas 1픽셀에 해당하는 월드 크기. 체력바 폭은 이 오브젝트의 RectTransform Width에서 설정합니다.")]
        [SerializeField, Min(0.001f)] private float worldUnitsPerPixel = 0.01f;

        [Header("Damage Lag")]
        [SerializeField] private bool enableDamageLag = true;
        [SerializeField] private Image damageLagFill;
        [SerializeField, Min(0f)] private float damageLagDelay = 0.08f;
        [SerializeField, Min(0.01f)] private float damageLagDuration = 0.35f;

        [Header("Death Animation")]
        [SerializeField] private bool enableDeathAnimation = true;
        [SerializeField, Min(0.01f)] private float deathTiltDuration = 0.09f;
        [SerializeField, Min(0.01f)] private float deathFadeDuration = 0.2f;

        private EnemyBase _enemy;
        private INewHealthModule _health;
        private EnemyVerticalFlipModule _verticalFlip;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _rect;
        private bool _started;

        private float _lagStartFill;
        private float _lagStartsAt;

        private bool _playingDeath;
        private float _deathStartedAt;
        private float _deathAngle;

        private void Awake()
        {
            _enemy = GetComponentInParent<EnemyBase>();
            _canvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _rect = GetComponent<RectTransform>();

            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.enabled = false;
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        private void Start()
        {
            // ModuleOwner의 Awake/모든 모듈 초기화가 끝난 뒤 조회합니다.
            _started = true;
            Bind();
        }

        private void OnEnable()
        {
            if (_started) Bind();
        }

        private void Bind()
        {
            Unbind();
            if (_enemy == null) return;

            _canvasGroup.alpha = 1f;
            _enemy.DeathStarted += OnDeathStarted;
            _health = _enemy.GetModule<INewHealthModule>();
            _verticalFlip = _enemy.GetModule<EnemyVerticalFlipModule>();

            if (_health != null)
            {
                _health.OnHealthChanged += OnHealthChanged;
                OnHealthChanged(_health.CurrentHealth, 0f, _health.MaxHealth);
            }

            Refresh();
        }

        private void OnHealthChanged(float current, float delta, float max)
        {
            float fill = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (healthFill != null) healthFill.fillAmount = fill;
            if (current <= 0f && !_playingDeath && !_enemy.IsWaitingForDeathLanding)
                _canvas.enabled = false;

            if (damageLagFill == null) return;

            if (enableDamageLag && delta < 0f && damageLagFill.fillAmount > fill)
            {
                _lagStartFill = damageLagFill.fillAmount;
                _lagStartsAt = Time.time + damageLagDelay;
            }
            else
            {
                damageLagFill.fillAmount = fill;
            }
        }

        private void UpdateDamageLag()
        {
            if (damageLagFill == null || healthFill == null) return;

            float target = healthFill.fillAmount;
            if (!enableDamageLag || damageLagFill.fillAmount <= target)
            {
                damageLagFill.fillAmount = target;
                damageLagFill.enabled = false;
                return;
            }

            if (!_enemy.IsWaitingForDeathLanding)
            {
                float progress = (Time.time - _lagStartsAt) / Mathf.Max(0.01f, damageLagDuration);
                damageLagFill.fillAmount = Mathf.SmoothStep(_lagStartFill, target, progress);
            }

            damageLagFill.enabled = damageLagFill.fillAmount > target;
        }

        private void OnDeathStarted()
        {
            if (!enableDeathAnimation || !_canvas.enabled)
            {
                _canvas.enabled = false;
                return;
            }

            _deathAngle = Random.Range(-70f, 70f);
            _deathStartedAt = Time.time;
            _lagStartsAt = _deathStartedAt + damageLagDelay;
            _playingDeath = true;
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (_playingDeath)
            {
                UpdateDeathAnimation();
                return;
            }

            bool visible = _enemy != null && _enemy.isActiveAndEnabled && _health != null &&
                (_enemy.IsWaitingForDeathLanding || (!_enemy.IsDead && !_health.IsDead && _health.CurrentHealth > 0f));
            _canvas.enabled = visible;
            if (!visible) return;

            UpdatePose(0f);
            UpdateDamageLag();
        }

        private void UpdateDeathAnimation()
        {
            float elapsed = Time.time - _deathStartedAt;
            float tiltDuration = Mathf.Max(0.01f, deathTiltDuration);
            float fade = (elapsed - tiltDuration) / Mathf.Max(0.01f, deathFadeDuration);
            if (!enableDeathAnimation || fade >= 1f)
            {
                _canvasGroup.alpha = 0f;
                _playingDeath = false;
                _canvas.enabled = false;
                return;
            }

            float angle = Mathf.SmoothStep(0f, _deathAngle, elapsed / tiltDuration);
            UpdatePose(angle);
            UpdateDamageLag();
            _canvasGroup.alpha = Mathf.SmoothStep(1f, 0f, fade);
        }

        private void UpdatePose(float angle)
        {
            Vector2 offset = worldOffset;
            if (_verticalFlip != null && _verticalFlip.IsFlipped) offset.y = -offset.y;
            _rect.SetPositionAndRotation(_enemy.transform.position + (Vector3)offset, Quaternion.Euler(0f, 0f, angle));

            Vector3 parentScale = _rect.parent != null ? _rect.parent.lossyScale : Vector3.one;
            float size = Mathf.Max(0.001f, worldUnitsPerPixel);
            // 루트 스케일/좌우 반전과 무관하게 바의 크기와 감소 방향을 유지합니다.
            _rect.localScale = new Vector3(ScaleForParent(size, parentScale.x),
                ScaleForParent(size, parentScale.y), ScaleForParent(size, parentScale.z));
        }

        private static float ScaleForParent(float size, float parentScale) =>
            Mathf.Abs(parentScale) > 0.0001f ? size / parentScale : size;

        private void Unbind()
        {
            if (_enemy != null) _enemy.DeathStarted -= OnDeathStarted;
            if (_health != null) _health.OnHealthChanged -= OnHealthChanged;
            _health = null;
            _playingDeath = false;
        }

        private void OnDisable()
        {
            Unbind();
            if (_canvas != null) _canvas.enabled = false;
        }

        private void OnDestroy() => Unbind();
    }
}
