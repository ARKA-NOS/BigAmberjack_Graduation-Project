using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.HitFeedback
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public sealed class EnemyDeathScreenFlash : MonoBehaviour
    {
        [SerializeField] private Image image;
        private CanvasGroup _group;
        private float _startedAt;
        private float _duration;
        private bool _playing;

        public void Show(Color color, float duration)
        {
            _group = GetComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;
            if (image == null) { Destroy(gameObject); return; }

            image.raycastTarget = false;
            image.color = color;
            _group.alpha = 1f;
            _duration = Mathf.Max(0.01f, duration);
            _startedAt = Time.unscaledTime;
            _playing = true;
        }

        private void Update()
        {
            if (!_playing) return;

            float t = Mathf.Clamp01((Time.unscaledTime - _startedAt) / _duration);
            _group.alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
