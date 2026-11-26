using System.Collections;
using _Scripts.UI.UI_System;
using UnityEngine;

namespace _Scripts.UI.Template
{
    public class FadeWindow : AbstractWindow
    {
        [SerializeField] private bool startSetting = false;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeSpeed = 3f;

        protected virtual void Awake()
        {
            canvasGroup.alpha = startSetting ? 1f : 0f;
            canvasGroup.interactable = startSetting;
        }

        private Coroutine coroutine;
        public override void Open(object data)
        {
            KillCoroutine();
            coroutine = StartCoroutine(Fade(1));
        }

        public override void Close()
        {
            KillCoroutine();
            coroutine = StartCoroutine(Fade(0));
        }

        private void KillCoroutine()
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;   
            }
        }
        
        // 다음에 열리는 창이 시간을 멈춰도(PausingWindow) 이 창의 페이드아웃이 끝나야 하므로 unscaled 시간을 쓴다.
        private IEnumerator Fade(float targetValue)
        {
            while (!Mathf.Approximately(canvasGroup.alpha, targetValue))
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetValue, fadeSpeed * Time.unscaledDeltaTime);
                yield return null;
            }

            canvasGroup.interactable = targetValue > 0;
        }


    }
}