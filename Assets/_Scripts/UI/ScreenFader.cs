using System;
using System.Collections;
using UnityEngine;

namespace SDW.Scripts.UI
{
    // 검은 화면으로 전환/복귀하는 간단한 Fade 연출.
    // ScreenFader 프리팹은 씬에 하나만 존재한다고 가정하며, 사용하는 쪽에서 SerializeField로 직접 참조한다.
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 0.3f;

        private CanvasGroup _canvasGroup;
        private Coroutine _routine;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        // 화면을 검게 덮은 뒤 onScreenCovered를 실행하고, 다시 화면을 밝힌다.
        public void Transition(Action onScreenCovered)
        {
            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(TransitionRoutine(onScreenCovered));
        }

        private IEnumerator TransitionRoutine(Action onScreenCovered)
        {
            yield return FadeTo(1f);

            onScreenCovered?.Invoke();

            yield return FadeTo(0f);

            _routine = null;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }
    }
}
