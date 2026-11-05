using Lrw.Script._Core._EventSystem;

namespace Lrw.Script.UI.CurrencyUI
{
    public static class CurrencyUIEvents
    {
        public static CurrencyUIActiveEvent CurrencyUIActive = new();
    }
    public class CurrencyUIActiveEvent : TemplateEvent<CurrencyUIActiveEvent, bool>
    {
        
    }
}