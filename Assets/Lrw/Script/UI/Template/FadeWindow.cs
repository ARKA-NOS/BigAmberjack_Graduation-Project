using System;
using System.Collections;
using UnityEngine;

namespace Lrw.Script.UI.Template
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
        
        private IEnumerator Fade(float targetValue)
        {
            float currentValue = canvasGroup.alpha;
            float dir = (targetValue - currentValue) > 0 ? 1 : -1;
            while (true)
            {
                currentValue = Mathf.Clamp01(currentValue + dir * fadeSpeed * Time.deltaTime);
                canvasGroup.alpha = currentValue;
                if(Mathf.Approximately(currentValue,targetValue)) break;
                yield return null;
            }

            canvasGroup.interactable = targetValue > 0;
        }


    }
}