using _Scripts._Core._EventSystem;

namespace _Scripts._Core.LanguageSystem
{
    public static class LanguageEvents
    {
        private static readonly LanguageChangeEvent LanguageChange = new();
        
        public static void RaiseLanguageChangeEvent(Language language)
            => EventBus<LanguageChangeEvent>.Raise(LanguageChange.Init(language));
    }
    
    public class LanguageChangeEvent : TemplateEvent<LanguageChangeEvent, Language>
    {}
    
}