using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.UI_System
{
    // 대상들을 화면 위 바깥에서 원래 위치로 내려오게 한다.
    // 숨김 거리는 해상도마다 캔버스 높이가 다르므로 고정값을 쓰지 않고 열고 닫을 때마다 계산한다.
    // 움직이는 도중에 반대로 바꾸면 현재 위치에서 이어서 움직인다.
    public class SlideWindowTransition : WindowTransition
    {
        [Tooltip("함께 움직일 대상. 레이아웃 그룹이 위치를 정하는 오브젝트는 넣지 않는다.")]
        [SerializeField] private RectTransform[] targets;
        [Tooltip("닫혔을 때 화면 위 끝에서 더 올려 두는 여유 거리. 그림자처럼 사각형 밖으로 나가는 효과를 가린다.")]
        [SerializeField, Min(0f)] private float hiddenMargin = 20f;
        [SerializeField, Min(0f)] private float showDuration = 0.25f;
        [SerializeField, Min(0f)] private float hideDuration = 0.15f;
        [SerializeField] private Ease showEase = Ease.OutCubic;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private readonly Vector3[] _corners = new Vector3[4];
        private Vector2[] _shownPositions;
        private bool _isShowing;

        public override void Show()
        {
            // 다 닫혀 있었다면 그사이 해상도가 바뀌었을 수 있으므로 숨김 위치를 다시 잡고 시작한다.
            if (!_isShowing && !IsMoving()) SnapHidden();

            _isShowing = true;
            Move(0f, showDuration, showEase);
        }

        public override void Hide()
        {
            _isShowing = false;
            Move(GetHiddenDistance(), hideDuration, hideEase);
        }

        public override void SnapHidden()
        {
            float distance = GetHiddenDistance();
            _isShowing = false;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;

                targets[i].DOKill();
                targets[i].anchoredPosition = GetPosition(i, distance);
            }
        }

        private void OnDestroy()
        {
            foreach (RectTransform target in targets)
            {
                if (target != null) target.DOKill();
            }
        }

        private void Move(float distance, float duration, Ease ease)
        {
            CachePositions();

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;

                targets[i].DOKill();
                targets[i].DOAnchorPos(GetPosition(i, distance), duration)
                    .SetEase(ease)
                    .SetUpdate(true);
            }
        }

        private bool IsMoving()
        {
            foreach (RectTransform target in targets)
            {
                if (target != null && DOTween.IsTweening(target)) return true;
            }

            return false;
        }

        // 원래 위치에서 캔버스 기준 distance만큼 위로 올린 anchoredPosition.
        private Vector2 GetPosition(int index, float distance)
        {
            RectTransform canvas = GetCanvas(targets[index]);
            if (canvas == null) return _shownPositions[index];

            Vector3 world = canvas.TransformVector(new Vector3(0f, distance, 0f));
            Vector2 local = targets[index].parent.InverseTransformVector(world);
            return _shownPositions[index] + local;
        }

        // 원래 위치에서 가장 아래에 있는 그래픽까지 화면 위로 나가려면 올려야 하는 거리(캔버스 단위).
        // 모든 대상이 같은 거리를 움직여야 서로의 배치가 유지된다.
        private float GetHiddenDistance()
        {
            CachePositions();

            float distance = 0f;
            for (int i = 0; i < targets.Length; i++)
            {
                RectTransform target = targets[i];
                if (target == null) continue;

                RectTransform canvas = GetCanvas(target);
                if (canvas == null) continue;

                float bottom = GetLowestY(target, canvas);
                if (float.IsPositiveInfinity(bottom)) continue;

                // 대상이 지금 원래 위치에서 벗어나 있으면 그만큼 되돌려서 잰다.
                Vector3 toShown = target.parent.TransformVector(_shownPositions[i] - target.anchoredPosition);
                bottom += canvas.InverseTransformVector(toShown).y;

                distance = Mathf.Max(distance, canvas.rect.yMax - bottom + hiddenMargin);
            }

            return distance;
        }

        // 실제로 그려지는 것 중 가장 낮은 y(캔버스 좌표). 마스크 안쪽은 마스크 밖으로 보이지 않으므로 마스크까지만 잰다.
        private float GetLowestY(RectTransform rect, RectTransform canvas)
        {
            float lowest = float.PositiveInfinity;
            if (!rect.gameObject.activeInHierarchy) return lowest;

            bool isMask = rect.TryGetComponent(out RectMask2D _) || rect.TryGetComponent(out Mask _);
            if (isMask || (rect.TryGetComponent(out Graphic graphic) && graphic.enabled))
            {
                rect.GetWorldCorners(_corners);
                foreach (Vector3 corner in _corners)
                    lowest = Mathf.Min(lowest, canvas.InverseTransformPoint(corner).y);
            }

            if (isMask) return lowest;

            foreach (Transform child in rect)
            {
                if (child is RectTransform childRect)
                    lowest = Mathf.Min(lowest, GetLowestY(childRect, canvas));
            }

            return lowest;
        }

        private static RectTransform GetCanvas(RectTransform target)
        {
            Canvas canvas = target.GetComponentInParent<Canvas>();
            return canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        }

        // 창의 Awake가 이 컴포넌트의 Awake보다 먼저 부를 수 있으므로, 처음 쓸 때 원래 위치를 기억한다.
        private void CachePositions()
        {
            if (_shownPositions != null) return;

            _shownPositions = new Vector2[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null) _shownPositions[i] = targets[i].anchoredPosition;
            }
        }
    }
}
