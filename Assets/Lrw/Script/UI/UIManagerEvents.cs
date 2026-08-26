using Lrw.Script._Core._EventSystem;

namespace Lrw.Script.UI
{
    public static class UIManagerEvents
    {
        public static readonly UIOpenCloseEvent UIOpenClose = new();
    }

    public class UIOpenCloseEvent : IEvent
    {
        public IWindow Window;

        public UIOpenCloseEvent Init(IWindow window)
        {
            Window = window;
            return this;
        }
    }
}