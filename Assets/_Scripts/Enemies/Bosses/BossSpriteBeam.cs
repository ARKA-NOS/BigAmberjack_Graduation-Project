using System.Collections.Generic;
using UnityEngine;

namespace CTJ.Enemies.Bosses
{
    // 원본 그림의 배치·페이드와 중심부 판정을 관리합니다. 피해량과 패턴 진행은 스킬의 책임입니다.
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
    public sealed class BossSpriteBeam : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float uniformScale = 0.75f;
        [Tooltip("원본 이미지 안의 발사구 위치(0~1). 왼쪽 아래가 (0,0)입니다.")]
        [SerializeField] private Vector2 muzzleUV = new Vector2(0.37f, 0.9f);
        [Tooltip("눈 중심의 FirePoint에서 그림 발사구까지의 월드 오프셋입니다.")]
        [SerializeField] private Vector2 muzzleOffset = new Vector2(0f, -1.1f);
        [Tooltip("원본에 그려진 빔 끝 중심(0~1). 눈동자가 바라보는 방향 계산에 사용합니다.")]
        [SerializeField] private Vector2 impactUV = new Vector2(0.6f, 0.06f);
        [Tooltip("원본 아래쪽의 접지 높이(0~1). 희미한 투명 여백 때문에 본체가 바닥 위에 떠 보이지 않게 합니다.")]
        [SerializeField, Range(0f, 0.2f)] private float floorContactV = 0.03f;
        [Header("Damage Core (Image UV)")]
        [Tooltip("흰 중심부 상단의 중심 좌표입니다. 바깥 광채는 피해 판정에서 제외합니다.")]
        [SerializeField] private Vector2 hitTopUV = new Vector2(0.43f, 0.84f);
        [Tooltip("흰 중심부 하단의 중심 좌표입니다. 바닥에 퍼지는 마지막 부분보다 위에 둡니다.")]
        [SerializeField] private Vector2 hitBottomUV = new Vector2(0.61f, 0.08f);
        [SerializeField, Range(0.01f, 0.5f)] private float hitHalfWidthUV = 0.13f;
        [Header("Quick Fade")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.08f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.12f;
        private SpriteRenderer _renderer;
        private PolygonCollider2D _hitbox;
        private readonly Vector2[] _corePath = new Vector2[4];
        private readonly List<Vector2> _points = new List<Vector2>();
        private float _sign = 1f;
        private bool _emitting, _fadingOut;
        private float _fadeStartedAt, _fadeFromAlpha;
        private float Scale => Mathf.Max(0.01f, uniformScale);
        public bool CanDamage => _emitting && _hitbox != null && _hitbox.enabled;
        public bool IsFadingOut => _fadingOut;
        public float Opacity => _renderer != null ? _renderer.color.a : 0f;
        public float HeightFromFloor => (LocalPoint(muzzleUV).y - LocalPoint(new Vector2(0.5f, floorContactV)).y) * Scale - muzzleOffset.y;
        public float FloorContactY => transform.TransformPoint(LocalPoint(new Vector2(0.5f, floorContactV))).y;
        public Vector2 WorldSize => (Vector2)Sprite.bounds.size * Scale;
        public Bounds Bounds => _hitbox != null ? _hitbox.bounds : default;
        public Vector2 Direction
        {
            get { Vector2 v = LocalPoint(impactUV) - LocalPoint(muzzleUV); v.x *= _sign; return v.normalized; }
        }
        private Sprite Sprite { get { Cache(); return _renderer.sprite; } }

        private void Awake() { Cache(); Hide(); }
        private void Cache()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_hitbox == null) _hitbox = GetComponent<PolygonCollider2D>();
        }

        public bool Configure(bool right)
        {
            Cache();
            if (_renderer.sprite == null) return false;
            float top = Mathf.Clamp01(hitTopUV.y), bottom = Mathf.Clamp01(hitBottomUV.y);
            if (top <= bottom) return false;
            float halfWidth = Mathf.Clamp(hitHalfWidthUV, 0.01f, 0.5f);
            _corePath[0] = LocalPoint(new Vector2(Mathf.Clamp01(hitTopUV.x - halfWidth), top));
            _corePath[1] = LocalPoint(new Vector2(Mathf.Clamp01(hitBottomUV.x - halfWidth), bottom));
            _corePath[2] = LocalPoint(new Vector2(Mathf.Clamp01(hitBottomUV.x + halfWidth), bottom));
            _corePath[3] = LocalPoint(new Vector2(Mathf.Clamp01(hitTopUV.x + halfWidth), top));
            _sign = right ? 1f : -1f;
            _renderer.flipX = !right;
            _renderer.flipY = false;
            _hitbox.isTrigger = true;
            // 명시적인 Overlap 검사에만 사용하고 플레이어/지형에 접촉 콜백을 보내지 않습니다.
            _hitbox.excludeLayers = ~0;
            _hitbox.callbackLayers = 0;
            _hitbox.contactCaptureLayers = 0;
            _hitbox.pathCount = 1;
            Vector2[] path = (Vector2[])_corePath.Clone();
            for (int i = 0; i < path.Length; i++) path[i].x *= _sign;
            _hitbox.SetPath(0, path);
            return true;
        }

        private Vector2 LocalPoint(Vector2 uv) => (Vector2.Scale(uv, Sprite.rect.size) - Sprite.pivot) / Sprite.pixelsPerUnit;
        private Vector2 CenterAt(Vector2 origin)
        {
            Vector2 anchor = LocalPoint(muzzleUV); anchor.x *= _sign;
            Vector2 offset = muzzleOffset; offset.x *= _sign;
            return origin + offset - anchor * Scale;
        }

        public Bounds WorldBoundsAt(Vector2 origin)
        {
            Vector2 center = Sprite.bounds.center; center.x *= _sign;
            return new Bounds(CenterAt(origin) + center * Scale, WorldSize);
        }

        public void Place(Vector2 origin)
        {
            transform.SetPositionAndRotation(CenterAt(origin), Quaternion.identity);
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            // 부모 크기를 보정하되, 월드 X/Y는 항상 같은 배율입니다. 추가 회전/세로 늘리기는 하지 않습니다.
            transform.localScale = new Vector3(Scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                Scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
        }

        public void Show(Vector2 origin)
        {
            Place(origin);
            if (!_emitting)
            {
                _emitting = true;
                _fadingOut = false;
                _fadeStartedAt = Time.time;
                SetOpacity(fadeInDuration <= 0f ? 1f : 0f);
                _hitbox.enabled = fadeInDuration <= 0f;
            }
            _renderer.enabled = true;
        }
        private void Update()
        {
            if (_emitting)
            {
                float t = fadeInDuration <= 0f ? 1f : Mathf.Clamp01((Time.time - _fadeStartedAt) / fadeInDuration);
                SetOpacity(Mathf.SmoothStep(0f, 1f, t));
                _hitbox.enabled = t >= 1f;
            }
            else if (_fadingOut)
            {
                float t = fadeOutDuration <= 0f ? 1f : Mathf.Clamp01((Time.time - _fadeStartedAt) / fadeOutDuration);
                SetOpacity(_fadeFromAlpha * (1f - Mathf.SmoothStep(0f, 1f, t)));
                if (t >= 1f) Hide();
            }
        }
        public void FadeOut()
        {
            if (_fadingOut) return;
            _emitting = false;
            _hitbox.enabled = false;
            if (!_renderer.enabled || fadeOutDuration <= 0f) { Hide(); return; }
            _fadingOut = true;
            _fadeFromAlpha = Opacity;
            _fadeStartedAt = Time.time;
        }
        private void SetOpacity(float alpha)
        {
            Color color = _renderer.color;
            color.a = alpha;
            _renderer.color = color;
        }
        public void OverlapAt(Vector2 origin, List<Collider2D> results)
        {
            Place(origin);
            Physics2D.SyncTransforms();
            _hitbox.Overlap(new ContactFilter2D { useTriggers = true }, results);
        }
        public void Hide()
        {
            Cache();
            _emitting = false;
            _fadingOut = false;
            SetOpacity(0f);
            _renderer.enabled = false;
            _hitbox.enabled = false;
        }
        private void OnDisable() => Hide();

        public void GetSweptHull(Vector2 start, Vector2 end, List<Vector2> hull)
        {
            _points.Clear();
            Vector2 startCenter = CenterAt(start), endCenter = CenterAt(end);
            float floorLocalY = LocalPoint(new Vector2(0.5f, floorContactV)).y;
            for (int i = 0; i < _corePath.Length; i++)
            {
                Vector2 p = _corePath[i];
                // 경고 하단은 실제 접지선까지 표시합니다. _corePath 자체를 바꾸지 않아
                // 바닥 끝을 제외한 피격 여유는 그대로 유지합니다.
                if (i == 1 || i == 2) p.y = Mathf.Min(p.y, floorLocalY);
                Vector2 offset = new Vector2(p.x * _sign, p.y) * Scale;
                _points.Add(startCenter + offset);
                _points.Add(endCenter + offset);
            }
            _points.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            hull.Clear();
            // 이동 전후 중심부 판정의 볼록 껍질. 장식용 바닥 퍼짐은 경고에서도 제외합니다.
            foreach (Vector2 p in _points)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], p - hull[hull.Count - 1]) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            int lowerCount = hull.Count;
            for (int i = _points.Count - 2; i >= 0; i--)
            {
                Vector2 p = _points[i];
                while (hull.Count > lowerCount && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], p - hull[hull.Count - 1]) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            if (hull.Count > 1) hull.RemoveAt(hull.Count - 1);
        }
        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
