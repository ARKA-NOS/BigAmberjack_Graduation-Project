using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Scripts.UI.Effects
{
    // 마우스를 올리면 살짝 커지고 벗어나면 돌아오는 연출. 버튼 등 아무 UI 오브젝트에 붙여 쓴다.
    // 같은 오브젝트에 누를 수 없는 상태의 Selectable이 있으면 커지지 않는다.
    public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Min(1f)] private float hoverScale = 1.1f;
        [Tooltip("다 커지거나 돌아오는 데 걸리는 시간(초).")]
        [SerializeField, Min(0.01f)] private float duration = 0.1f;

        private Vector3 _baseScale;
        private Selectable _selectable;
        private bool _hovered;
        // 0이면 원래 크기, 1이면 hoverScale 크기.
        private float _progress;

        // 다른 컴포넌트가 상태에 따라 확대를 막을 때 끈다(예: 잠긴 테크트리 노드).
        public bool CanGrow { get; set; } = true;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _selectable = GetComponent<Selectable>();
        }

        // 꺼질 때는 PointerExit가 오지 않을 수 있으므로 바로 되돌린다.
        private void OnDisable()
        {
            _hovered = false;
            _progress = 0f;
            transform.localScale = _baseScale;
        }

        public void OnPointerEnter(PointerEventData eventData) => _hovered = true;

        public void OnPointerExit(PointerEventData eventData) => _hovered = false;

        private void Update()
        {
            bool grow = _hovered && CanGrow && (_selectable == null || _selectable.IsInteractable());
            float target = grow ? 1f : 0f;
            if (Mathf.Approximately(_progress, target)) return;

            _progress = Mathf.MoveTowards(_progress, target, Time.unscaledDeltaTime / duration);
            transform.localScale = _baseScale * Mathf.Lerp(1f, hoverScale, _progress);
        }
    }
}
