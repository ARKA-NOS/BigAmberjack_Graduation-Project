using System.Collections;
using UnityEngine;

namespace Lrw.Script.UI.UpgradeUI
{
    public class TestFadeUI : AbstractWindow
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeSpeed = 3f;
        
        private Coroutine coroutine;
        public override void Open(object data)
        {
            coroutine = StartCoroutine(Fade(1));
        }

        public override void Close()
        {
            coroutine = StartCoroutine(Fade(2));
        }

        private IEnumerator Fade(float targetValue)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;   
            }
            
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