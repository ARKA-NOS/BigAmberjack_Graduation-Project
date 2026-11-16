using _Scripts._Core._EventSystem;
using _Scripts._Core._Manager;
using _Scripts._Core._SaveSystem;
using UnityEngine;

namespace _Scripts._Core.LanguageSystem
{
    public class LanguageManager : AbstractManager
    {
        [SerializeField] private Language currentLanguage = Language.한국어;
        
        private const string SaveName = "Language";
        
        public override void Initialize()
        {
            EventBus<LanguageChangeEvent>.Subscribe(HandleLanguageChangeEvent);
        }
        
        private void Start()
        {
            currentLanguage = SaveSystem.Load(SaveName, new LanguageSaveData(currentLanguage)).languageData;
            LanguageEvents.RaiseLanguageChangeEvent(currentLanguage);
        }
        
        public override void GameEnd()
        {
            EventBus<LanguageChangeEvent>.UnSubscribe(HandleLanguageChangeEvent);
            SaveSystem.Save(SaveName, new LanguageSaveData(currentLanguage));
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