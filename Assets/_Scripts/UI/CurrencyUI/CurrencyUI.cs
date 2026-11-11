using _Scripts._Core._EventSystem;
using UnityEngine;

namespace _Scripts.UI.CurrencyUI
{
    public class CurrencyUI : MonoBehaviour
    {
        private void Awake()
        {
            EventBus<CurrencyUIActiveEvent>.Subscribe(HandleCurrencyActive);
        }

        private void OnDestroy()
        {
            EventBus<CurrencyUIActiveEvent>.UnSubscribe(HandleCurrencyActive);
        }

        private void HandleCurrencyActive(CurrencyUIActiveEvent evt)
        {
            gameObject.SetActive(evt.Value);
        }
        
    }
}