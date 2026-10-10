using Lrw.Script._Core._EventSystem;

namespace Lrw.Script.UI
{
    public static class UIManagerEvents
    {
        public static readonly UIOpenCloseEvent UIOpenClose = new();
        
        public static void OpenCloseWindow(IWindow window,object data = null)
            => EventBus<UIOpenCloseEvent>.Raise(UIOpenClose.Init(window,data));
        
    }

    public class UIOpenCloseEvent : IEvent
    {
        public IWindow Window;
        public object Data;
        public UIOpenCloseEvent Init(IWindow window,object data)
        {
            Window = window;
            Data = data;
            return this;
        }

        
    }
}