using _Scripts._Core._EventSystem;

namespace _Scripts.UI.CurrencyUI
{
    public static class CurrencyUIEvents
    {
        public static CurrencyUIActiveEvent CurrencyUIActive = new();
    }
    public class CurrencyUIActiveEvent : TemplateEvent<CurrencyUIActiveEvent, bool>
    {
        
    }
}