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
        [Tooltip("일반 기본 크기를 기준으로 생성 시 한 번 선택하는 배율입니다.")]
        [SerializeField] private Vector2 _normalSizeRange = new Vector2(0.8f, 1f);
        [Tooltip("가로축은 전체 수명의 비율, 세로축은 기본 크기의 배율입니다.")]
        [SerializeField] private AnimationCurve _normalScale = new AnimationCurve(
            new Keyframe(0f, 1.45f), new Keyframe(0.12f, 0.8f),
            new Keyframe(0.26f, 1.12f), new Keyframe(0.45f, 1f), new Keyframe(1f, 1f));

        [Header("Critical Display Only")]
        [SerializeField] private Color _criticalColor = new Color(1f, 0.84f, 0.12f, 1f);
        [Tooltip("일반 기본 크기를 기준으로 선택합니다. 일반 무작위 배율을 중복 적용하지 않습니다.")]
        [SerializeField] private Vector2 _criticalSizeRange = new Vector2(1.3f, 1.6f);
        [SerializeField, Min(0.1f)] private float _criticalDurationMultiplier = 1.1f;
        [SerializeField] private AnimationCurve _criticalScale = new AnimationCurve(
            new Keyframe(0f, 1.8f), new Keyframe(0.12f, 0.65f),
            new Keyframe(0.26f, 1.4f), new Keyframe(0.45f, 1f), new Keyframe(1f, 1f));

        [Header("Rise / Fade (World Units)")]
        [SerializeField, Min(0f)] private float _riseDistance = 1.1f;
        [SerializeField, Min(1f)] private float _criticalRiseMultiplier = 1.2f;
        [SerializeField, Range(0f, 0.9f)] private float _riseStart = 0.22f;
        [SerializeField, Range(0f, 0.95f)] private float _fadeStart = 0.55f;
        [Tooltip("기준 위치에서 공격 진행 방향 쪽 반원 안에 생성할 최대 거리입니다. 방향이 없으면 원 전체를 사용합니다.")]
        [SerializeField, Min(0f)] private float _spawnRadius = 0.5f;
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
        private float _sizeMultiplier;

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
            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, _spawnRadius);
            Vector2 direction = data.Direction;

            if (HasDirection(direction))
            {
                direction.Normalize();
                float alongDirection = Vector2.Dot(offset, direction);

                // 뒤쪽에서 뽑힌 위치를 대칭 이동하여 거리와 분포를 유지합니다.
                if (alongDirection < 0f) offset -= 2f * alongDirection * direction;
            }

            _origin = data.Position + (Vector3)offset;

            Vector2 sizeRange = _critical ? _criticalSizeRange : _normalSizeRange;
            float minimum = Mathf.Max(0.01f, Mathf.Min(sizeRange.x, sizeRange.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(sizeRange.x, sizeRange.y));

            _sizeMultiplier = Random.Range(minimum, maximum);
            _drift = Random.Range(-_horizontalDrift, _horizontalDrift);
            _age = 0f;
            _startedAt = Time.time;
            double roundedDamage = System.Math.Round(data.Damage, System.MidpointRounding.AwayFromZero);

            _label.text = roundedDamage.ToString("0", CultureInfo.InvariantCulture);
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
            float size = _sizeMultiplier * Mathf.Max(0.01f, curve.Evaluate(t));

            transform.localScale = _baseScale * size;
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_riseStart, 1f, t));
            float height = _riseDistance * (_critical ? _criticalRiseMultiplier : 1f);

            transform.position = _origin + new Vector3(_drift * rise, height * rise, 0f);
            _group.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_fadeStart, 1f, t));
        }

        private static bool HasDirection(Vector2 value) =>
            !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.x) &&
            !float.IsInfinity(value.y) && value.sqrMagnitude > 0.0001f;
    }
}
