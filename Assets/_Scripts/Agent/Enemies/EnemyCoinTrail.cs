using UnityEngine;
using UnityEngine.Rendering;

namespace _Scripts.Agent.Enemies
{
    // 코인의 이동/정산과 분리된 잔상 연출. 이 컴포넌트를 제거해도 코인 보상은 동작합니다.
    [DisallowMultipleComponent]
    public sealed class EnemyCoinTrail : MonoBehaviour
    {
        [SerializeField] private Material trailMaterial;
        [Tooltip("이동 경로가 남는 시간(초)입니다.")]
        [SerializeField, Min(0.01f)] private float trailLifetime = 0.45f;
        [Tooltip("가장 두꺼운 부분의 월드 유닛 너비. 코인의 도착 펀치 스케일과 독립적입니다.")]
        [SerializeField, Min(0.001f)] private float trailWidth = 0.16f;
        [Tooltip("왼쪽(0)은 코인 쪽, 오른쪽(1)은 꼬리 끝입니다. 끝 값을 0으로 두면 한 점으로 모입니다.")]
        [SerializeField] private AnimationCurve widthProfile = new AnimationCurve(
            new Keyframe(0f, 0.8f), new Keyframe(0.12f, 1f),
            new Keyframe(0.4f, 0.65f), new Keyframe(0.75f, 0.22f), new Keyframe(1f, 0f));
        [SerializeField] private Color headColor = new Color(1f, 0.92f, 0.45f, 0.45f);
        [SerializeField] private Color tailColor = new Color(1f, 0.42f, 0.06f, 0.06f);
        [Tooltip("경로를 기록할 최소 간격(월드 유닛). 작을수록 곡선이 부드럽고 정점 수가 많아집니다.")]
        [SerializeField, Min(0.005f)] private float minVertexDistance = 0.035f;
        [Tooltip("플레이어에게 도착한 뒤 남은 트레일이 사라지는 시간. 코인의 지급 시점에는 영향을 주지 않습니다.")]
        [SerializeField, Min(0.01f)] private float collectFadeDuration = 0.16f;
        [SerializeField] private int sortingOrderOffset = -1;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private TrailRenderer _trail;
        private SpriteRenderer _visual;
        private MaterialPropertyBlock _properties;
        private bool _following;
        private bool _fading;
        private float _fadeElapsed;

        public void BeginTrail(SpriteRenderer visual)
        {
            _visual = visual;
            _following = true;
            _fading = false;
            if (!isActiveAndEnabled || trailMaterial == null) return;

            if (_trail == null)
            {
                var child = new GameObject("CoinTrail");
                child.layer = gameObject.layer;
                child.transform.SetParent(transform, false);
                _trail = child.AddComponent<TrailRenderer>();
                _properties = new MaterialPropertyBlock();
            }

            _trail.emitting = false;
            _trail.Clear(); // 재활성화/재사용 때 이전 위치까지 선이 이어지는 것을 방지합니다.
            _trail.sharedMaterial = trailMaterial;
            _trail.time = Mathf.Max(0.01f, trailLifetime);
            _trail.widthCurve = widthProfile;
            _trail.widthMultiplier = Mathf.Max(0.001f, trailWidth);
            _trail.minVertexDistance = Mathf.Max(0.005f, minVertexDistance);
            _trail.numCapVertices = 4;
            _trail.numCornerVertices = 3;
            _trail.alignment = LineAlignment.View;
            _trail.textureMode = LineTextureMode.Stretch;
            _trail.autodestruct = false;
            _trail.shadowCastingMode = ShadowCastingMode.Off;
            _trail.receiveShadows = false;
            _trail.lightProbeUsage = LightProbeUsage.Off;
            _trail.reflectionProbeUsage = ReflectionProbeUsage.Off;
            if (_visual != null)
            {
                _trail.sortingLayerID = _visual.sortingLayerID;
                _trail.sortingOrder = _visual.sortingOrder + sortingOrderOffset;
            }

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(headColor, 0f), new GradientColorKey(tailColor, 1f) },
                new[] { new GradientAlphaKey(headColor.a, 0f), new GradientAlphaKey(tailColor.a, 1f) });
            _trail.colorGradient = gradient;
            SetOpacity(1f);
            _trail.enabled = true;
            _trail.emitting = true;
        }

        public void StopTrail()
        {
            _following = false;
            if (_trail == null) return;
            _trail.emitting = false; // 도착 후 플레이어를 따라 새 꼬리를 만들지 않습니다.
            if (_fading) return;
            _fadeElapsed = 0f;
            _fading = true;
        }

        private void LateUpdate()
        {
            if (!_fading || _trail == null) return;
            _fadeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeElapsed / Mathf.Max(0.01f, collectFadeDuration));
            SetOpacity(1f - Mathf.SmoothStep(0f, 1f, t));
            if (t < 1f) return;
            _fading = false;
            _trail.Clear();
            _trail.enabled = false;
        }

        private void SetOpacity(float alpha)
        {
            // 공유 머티리얼을 복제하거나 바꾸지 않고 이 코인의 트레일만 페이드합니다.
            _properties.SetColor(ColorId, new Color(1f, 1f, 1f, alpha));
            _trail.SetPropertyBlock(_properties);
        }

        private void OnEnable()
        {
            if (_following) BeginTrail(_visual);
        }

        private void OnDisable()
        {
            _fading = false;
            if (_trail == null) return;
            _trail.emitting = false;
            _trail.Clear();
            _trail.enabled = false;
        }

        private void OnDestroy()
        {
            // 컴포넌트만 제거한 경우에도 런타임 자식이 남지 않게 합니다.
            if (_trail != null) Destroy(_trail.gameObject);
        }
    }
}
