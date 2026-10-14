using UnityEngine;
using UnityEngine.UI;

namespace CTJ.UI
{
    // 적 스킬에서 호출하지만 판정이나 피해 처리는 하지 않는 재사용 가능한 Canvas 표시입니다.
    [RequireComponent(typeof(Canvas), typeof(RectTransform))]
    public sealed class AttackTelegraphUI : MonoBehaviour
    {
        [SerializeField] private Image areaImage;
        [SerializeField] private Color areaColor = new Color32(255, 110, 110, 50);
        [SerializeField, Min(0f)] private float blinkDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float blinkInterval = 0.04f;
        private Canvas _canvas;
        private RectTransform _rect;
        private float _blinkStartedAt;

        public bool IsVisible { get; private set; }
        public bool IsBlinking { get; private set; }
        public float BlinkDuration => Mathf.Max(0f, blinkDuration);

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _rect = GetComponent<RectTransform>();
            _canvas.renderMode = RenderMode.WorldSpace;
            if (areaImage != null) areaImage.raycastTarget = false;
            Hide();
        }

        public void Show()
        {
            IsVisible = true;
            IsBlinking = false;
            _canvas.enabled = true;
            if (areaImage != null) areaImage.color = areaColor;
        }

        public void BeginBlink()
        {
            if (!IsVisible || IsBlinking || BlinkDuration <= 0f) return;
            IsBlinking = true;
            _blinkStartedAt = Time.time;
        }

        public void Hide()
        {
            IsVisible = false;
            IsBlinking = false;
            if (_canvas != null) _canvas.enabled = false;
        }

        private void Update()
        {
            if (!IsVisible || areaImage == null) return;
            Color color = areaColor;
            if (IsBlinking && Mathf.FloorToInt((Time.time - _blinkStartedAt) / Mathf.Max(0.01f, blinkInterval)) % 2 == 0)
                color.a = 0f;
            areaImage.color = color;
        }

        public void SetBox(BoxCollider2D box)
        {
            Vector3 scale = box.transform.lossyScale;
            SetWorldRect(box.transform.TransformPoint(box.offset),
                new Vector2(box.size.x * Mathf.Abs(scale.x), box.size.y * Mathf.Abs(scale.y)), box.transform.rotation);
        }

        public void SetBounds(Bounds bounds) => SetWorldRect(bounds.center, bounds.size, Quaternion.identity);

        public void SetLane(Vector2 origin, Vector2 direction, float distance, float width)
        {
            direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            distance = Mathf.Max(0f, distance);
            width = Mathf.Max(0.01f, width);
            SetWorldRect(origin + direction * distance * 0.5f, new Vector2(distance + width, width),
                Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
        }

        private void SetWorldRect(Vector3 center, Vector2 size, Quaternion rotation)
        {
            _rect.SetPositionAndRotation(center, rotation);
            // 적 프리팹의 1배/2배 스케일이 월드 크기에 다시 곱해지지 않도록 보정합니다.
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            _rect.localScale = new Vector3(InverseScale(parentScale.x), InverseScale(parentScale.y), InverseScale(parentScale.z));
            _rect.sizeDelta = size;
        }

        private static float InverseScale(float value) => Mathf.Abs(value) > 0.0001f ? 1f / value : 1f;
        private void OnDisable() => Hide();
    }
}
