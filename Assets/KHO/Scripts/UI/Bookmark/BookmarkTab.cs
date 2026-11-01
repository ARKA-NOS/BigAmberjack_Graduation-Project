using System;
using UnityEngine;
using UnityEngine.UI;

namespace KHO.Scripts.UI.Bookmark
{
    // 창 옆에 붙은 책갈피 하나. 선택되면 바깥쪽으로 더 길게 튀어나온다.
    [RequireComponent(typeof(Button))]
    public class BookmarkTab : MonoBehaviour
    {
        [SerializeField] private float selectedLengthRatio = 1.4f;

        public event Action<BookmarkTab> OnClicked;

        private RectTransform _rectTransform;
        private float _baseAnchorMaxX;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _baseAnchorMaxX = _rectTransform.anchorMax.x;
            GetComponent<Button>().onClick.AddListener(() => OnClicked?.Invoke(this));
        }

        public void SetSelected(bool selected)
        {
            float baseLength = _baseAnchorMaxX - _rectTransform.anchorMin.x;
            float length = selected ? baseLength * selectedLengthRatio : baseLength;
            _rectTransform.anchorMax = new Vector2(_rectTransform.anchorMin.x + length, _rectTransform.anchorMax.y);
        }
    }
}
