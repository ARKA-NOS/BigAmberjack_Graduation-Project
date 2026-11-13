using _Scripts._Core._EventSystem;
using _Scripts._Core._Manager;
using UnityEngine;

namespace _Scripts._Core.LanguageSystem
{
    public class LanguageManager : AbstractManager
    {
        [SerializeField] private Language currentLanguage = Language.한국어;
        
        public override void Initialize()
        {
            EventBus<LanguageChangeEvent>.Subscribe(HandleLanguageChangeEvent);
        }

        private void Start()
        {
            LanguageEvents.RaiseLanguageChangeEvent(currentLanguage);
        }

        public override void GameEnd()
        {
            EventBus<LanguageChangeEvent>.UnSubscribe(HandleLanguageChangeEvent);
        }
        
        private void HandleLanguageChangeEvent(LanguageChangeEvent evt)
        {
            currentLanguage = evt.Value;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            LanguageEvents.RaiseLanguageChangeEvent(currentLanguage);
        }
#endif
        
    }
}