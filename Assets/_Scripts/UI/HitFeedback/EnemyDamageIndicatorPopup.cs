using System.Globalization;
using TMPro;
using UnityEngine;

namespace _Scripts.UI.HitFeedback
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public sealed class EnemyDamageIndicatorPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [Header("Normal")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField, Min(0.05f)] private float _duration = 0.8f;
        [Tooltip("가로축은 전체 수명의 비율, 세로축은 기본 크기의 배율입니다.")]
        [SerializeField] private AnimationCurve _normalScale = new AnimationCurve(
            new Keyframe(0f, 1.45f), new Keyframe(0.12f, 0.8f),
            new Keyframe(0.26f, 1.12f), new Keyframe(0.45f, 1f), new Keyframe(1f, 1f));
        [Header("Critical Display Only")]
        [SerializeField] private Color _criticalColor = new Color(1f, 0.84f, 0.12f, 1f);
        [SerializeField, Min(1f)] private float _criticalSizeMultiplier = 1.25f;
        [SerializeField, Min(0.1f)] private float _criticalDurationMultiplier = 1.1f;
        [SerializeField] private AnimationCurve _criticalScale = new AnimationCurve(
            new Keyframe(0f, 1.8f), new Keyframe(0.12f, 0.65f),
            new Keyframe(0.26f, 1.4f), new Keyframe(0.45f, 1f), new Keyframe(1f, 1f));
        [Header("Rise / Fade (World Units)")]
        [SerializeField, Min(0f)] private float _riseDistance = 1.1f;
        [SerializeField, Min(1f)] private float _criticalRiseMultiplier = 1.2f;
        [SerializeField, Range(0f, 0.9f)] private float _riseStart = 0.22f;
        [SerializeField, Range(0f, 0.95f)] private float _fadeStart = 0.55f;
        [SerializeField, Min(0f)] private float _horizontalSpread = 0.16f;
        [SerializeField, Min(0f)] private float _horizontalDrift = 0.12f;

        private CanvasGroup _group;
        private Vector3 _baseScale;
        private Vector3 _origin;
        private bool _critical;
        private bool _playing;
        private float _age;
        private float _startedAt;
        private float _lifetime;
        private float _drift;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _group = GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            if (_label != null) _label.raycastTarget = false;
        }

        public void Show(DamageIndicatorData data)
        {
            if (_label == null || data.Damage <= 0f || float.IsNaN(data.Damage) || float.IsInfinity(data.Damage))
            {
                Destroy(gameObject);
                return;
            }
            _critical = data.IsCritical;
            _lifetime = Mathf.Max(0.05f, _duration * (_critical ? _criticalDurationMultiplier : 1f));
            _origin = data.Position + Vector3.right * Random.Range(-_horizontalSpread, _horizontalSpread);
            _drift = Random.Range(-_horizontalDrift, _horizontalDrift);
            _age = 0f;
            _startedAt = Time.time;
            _label.text = Mathf.Max(0.1f, data.Damage).ToString("0.#", CultureInfo.InvariantCulture);
            _label.color = _critical ? _criticalColor : _normalColor;
            _playing = true;
            ApplyFrame(0f); // 첫 화면부터 크게 표시하도록 다음 Update까지 기다리지 않습니다.
        }

        private void Update()
        {
            if (!_playing) return;
            // 생성된 프레임 전체의 deltaTime을 더하면 첫 큰 자세가 즉시 건너뛰어질 수 있습니다.
            _age = Time.time - _startedAt;
            float t = Mathf.Clamp01(_age / _lifetime);
            ApplyFrame(t);
            if (t >= 1f) Destroy(gameObject);
        }

        private void ApplyFrame(float t)
        {
            AnimationCurve curve = _critical ? _criticalScale : _normalScale;
            float size = (_critical ? _criticalSizeMultiplier : 1f) * Mathf.Max(0.01f, curve.Evaluate(t));
            transform.localScale = _baseScale * size;
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_riseStart, 1f, t));
            float height = _riseDistance * (_critical ? _criticalRiseMultiplier : 1f);
            transform.position = _origin + new Vector3(_drift * rise, height * rise, 0f);
            _group.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_fadeStart, 1f, t));
        }
    }
}
