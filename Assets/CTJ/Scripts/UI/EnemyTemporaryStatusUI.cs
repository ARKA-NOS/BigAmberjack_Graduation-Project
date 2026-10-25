using CTJ.Enemies;
using Lrw.Script.Agent.HealthSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CTJ.UI
{
    // 임시 표시 전용. 적/모듈에서 이 UI를 참조하지 않으므로 자식 오브젝트째 삭제해도 전투는 유지됩니다.
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas), typeof(RectTransform))]
    public sealed class EnemyTemporaryStatusUI : MonoBehaviour
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private Image reactionFill;
        [Tooltip("적 루트에서 UI까지의 월드 좌표 오프셋. 방향 전환/피격 스케일과 분리됩니다.")]
        [SerializeField] private Vector2 worldOffset = new Vector2(0f, 1.5f);
        [Tooltip("Canvas 1픽셀에 해당하는 월드 크기. 기본 폭 160픽셀 × 0.01 = 1.6 유닛입니다.")]
        [SerializeField, Min(0.001f)] private float worldUnitsPerPixel = 0.01f;

        private EnemyBase _enemy;
        private IHealthModule _health;
        private EnemyHitReactionModule _reaction;
        private Canvas _canvas;
        private RectTransform _rect;
        private bool _started;

        private void Awake()
        {
            _enemy = GetComponentInParent<EnemyBase>();
            _canvas = GetComponent<Canvas>();
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
            _health = _enemy.GetModule<IHealthModule>();
            _reaction = _enemy.GetModule<EnemyHitReactionModule>();
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
            if (reactionFill != null)
                reactionFill.fillAmount = _reaction != null ? _reaction.ReactionRemainingNormalized : 0f;
            bool visible = _enemy != null && _enemy.isActiveAndEnabled && !_enemy.IsDead &&
                _health != null && !_health.IsDead && _health.CurrentHealth > 0f;
            _canvas.enabled = visible;
            if (!visible) return;

            _rect.SetPositionAndRotation(_enemy.transform.position + (Vector3)worldOffset, Quaternion.identity);
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
            if (_health != null) _health.OnHealthChanged -= OnHealthChanged;
            _health = null;
            _reaction = null;
        }

        private void OnDisable()
        {
            Unbind();
            if (_canvas != null) _canvas.enabled = false;
        }

        private void OnDestroy() => Unbind();
    }
}
