using System;

namespace _Scripts._Core.LanguageSystem
{
    [Serializable]
    public struct LanguageSaveData
    {
        public Language languageData;

        public LanguageSaveData(Language language)
        {
            languageData = language;
        }
    }
}