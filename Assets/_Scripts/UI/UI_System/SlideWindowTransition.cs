using DG.Tweening;
using UnityEngine;

namespace _Scripts.UI.UI_System
{
    // 대상들을 숨김 위치(원래 위치 + hiddenOffset)에서 원래 위치로 미끄러뜨린다.
    // 움직이는 도중에 반대로 바꾸면 현재 위치에서 이어서 움직인다.
    public class SlideWindowTransition : WindowTransition
    {
        [Tooltip("함께 움직일 대상. 레이아웃 그룹이 위치를 정하는 오브젝트는 넣지 않는다.")]
        [SerializeField] private RectTransform[] targets;
        [Tooltip("닫혔을 때 원래 위치에서 떨어진 거리. 오른쪽에서 들어오면 x를, 위에서 내려오면 y를 양수로 둔다.")]
        [SerializeField] private Vector2 hiddenOffset;
        [SerializeField, Min(0f)] private float showDuration = 0.25f;
        [SerializeField, Min(0f)] private float hideDuration = 0.15f;
        [SerializeField] private Ease showEase = Ease.OutCubic;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private Vector2[] _shownPositions;

        public override void Show() => Move(Vector2.zero, showDuration, showEase);

        public override void Hide() => Move(hiddenOffset, hideDuration, hideEase);

        public override void SnapHidden()
        {
            CachePositions();

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;

                targets[i].DOKill();
                targets[i].anchoredPosition = _shownPositions[i] + hiddenOffset;
            }
        }

        private void OnDestroy()
        {
            foreach (RectTransform target in targets)
            {
                if (target != null) target.DOKill();
            }
        }

        private void Move(Vector2 offset, float duration, Ease ease)
        {
            CachePositions();

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;

                targets[i].DOKill();
                targets[i].DOAnchorPos(_shownPositions[i] + offset, duration)
                    .SetEase(ease)
                    .SetUpdate(true);
            }
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
