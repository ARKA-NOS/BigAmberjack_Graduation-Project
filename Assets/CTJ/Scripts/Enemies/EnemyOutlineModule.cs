using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using UnityEngine;

namespace CTJ.Enemies
{
    [DisallowMultipleComponent, DefaultExecutionOrder(200)]
    public sealed class EnemyOutlineModule : Module, IAfterInitModule
    {
        [SerializeField] private SpriteRenderer[] sprites;
        [SerializeField] private Material outlineMaterial;
        [SerializeField, ColorUsage(false)] private Color normalColor = new Color(0.55f, 0.08f, 0.08f, 1f);
        [SerializeField, ColorUsage(false)] private Color hitColor = Color.white;
        [Tooltip("화면 픽셀 기준으로 실루엣 안쪽 테두리를 표시합니다. 0이면 숨깁니다.")]
        [SerializeField, Range(0f, 6f)] private float width = 2f;
        [Tooltip("HP가 감소한 뒤 피격 색을 유지할 시간. 연타 시 마지막 피격부터 다시 계산합니다.")]
        [SerializeField, Min(0f)] private float hitDuration = 0.25f;

        private sealed class View
        {
            public SpriteRenderer Source;
            public SpriteRenderer Outline;
        }

        private EnemyBase _enemy;
        private INewHealthModule _health;
        private View[] _views;
        private bool _initialized;
        private bool _subscribed;
        private float _hitUntil;
        private MaterialPropertyBlock _properties;
        private readonly Dictionary<Sprite, Vector4> _uvBounds = new Dictionary<Sprite, Vector4>();
        private static readonly int WidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int RectId = Shader.PropertyToID("_SpriteUVRect");

        public bool IsShowingHit => isActiveAndEnabled && Time.time < _hitUntil;
        public Color CurrentColor => IsShowingHit ? hitColor : normalColor;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _health = owner.GetModule<INewHealthModule>();
            if (sprites == null || sprites.Length == 0) sprites = owner.GetComponentsInChildren<SpriteRenderer>(true);
        }

        public void AfterInit()
        {
            _initialized = true;
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable()
        {
            if (_initialized) Subscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _health == null) return;
            _health.OnHealthChanged += OnHealthChanged;
            _subscribed = true;
        }

        private void OnHealthChanged(float current, float delta, float maximum)
        {
            if (current <= 0f) _hitUntil = 0f;
            else if (delta < 0f) _hitUntil = Time.time + Mathf.Max(0f, hitDuration);
        }

        private void CreateViews()
        {
            // 모든 모듈 초기화 뒤 생성해 다른 모듈의 SpriteRenderer 자동 수집에 섞이지 않게 합니다.
            _properties = new MaterialPropertyBlock();
            _views = new View[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                SpriteRenderer source = sprites[i];
                if (source == null) continue;
                GameObject child = new GameObject("TemporaryOutline") { hideFlags = HideFlags.DontSave };
                child.transform.SetParent(source.transform, false);
                SpriteRenderer outline = child.AddComponent<SpriteRenderer>();
                outline.enabled = false;
                _views[i] = new View { Source = source, Outline = outline };
            }
        }

        private void LateUpdate()
        {
            if (!_initialized) return;
            if (_views == null) CreateViews();
            bool visible = _enemy != null && _enemy.isActiveAndEnabled && !_enemy.IsDead &&
                           outlineMaterial != null && width > 0f;
            Color color = CurrentColor;
            foreach (View view in _views)
            {
                if (view == null || view.Outline == null) continue;
                SpriteRenderer source = view.Source, outline = view.Outline;
                outline.enabled = visible && source != null && source.enabled && source.gameObject.activeInHierarchy && source.sprite != null;
                if (!outline.enabled) continue;
                outline.gameObject.layer = source.gameObject.layer;
                outline.sprite = source.sprite;
                outline.flipX = source.flipX;
                outline.flipY = source.flipY;
                outline.drawMode = source.drawMode;
                outline.size = source.size;
                outline.tileMode = source.tileMode;
                outline.maskInteraction = source.maskInteraction;
                outline.sortingLayerID = source.sortingLayerID;
                outline.sortingOrder = source.sortingOrder + 1;
                outline.spriteSortPoint = source.spriteSortPoint;
                outline.sharedMaterial = outlineMaterial;
                Color tint = color;
                tint.a *= source.color.a;
                outline.color = tint;
                _properties.Clear();
                _properties.SetFloat(WidthId, Mathf.Clamp(width, 0f, 6f));
                _properties.SetVector(RectId, GetUVBounds(source.sprite));
                outline.SetPropertyBlock(_properties);
            }
        }

        private Vector4 GetUVBounds(Sprite sprite)
        {
            if (_uvBounds.TryGetValue(sprite, out Vector4 bounds)) return bounds;
            bounds = new Vector4(1f, 1f, 0f, 0f);
            foreach (Vector2 uv in sprite.uv)
            {
                bounds.x = Mathf.Min(bounds.x, uv.x); bounds.y = Mathf.Min(bounds.y, uv.y);
                bounds.z = Mathf.Max(bounds.z, uv.x); bounds.w = Mathf.Max(bounds.w, uv.y);
            }
            _uvBounds.Add(sprite, bounds);
            return bounds;
        }

        private void OnDisable()
        {
            if (_subscribed) _health.OnHealthChanged -= OnHealthChanged;
            _subscribed = false;
            _hitUntil = 0f;
            if (_views == null) return;
            foreach (View view in _views)
                if (view?.Outline != null) view.Outline.enabled = false;
        }

        private void OnDestroy()
        {
            OnDisable();
            if (_views == null) return;
            foreach (View view in _views)
                if (view?.Outline != null) Destroy(view.Outline.gameObject);
        }
    }
}
