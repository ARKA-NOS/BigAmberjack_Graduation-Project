using Lrw.Script._Core._EventSystem;
using Lrw.Script._Core._Manager;

namespace Lrw.Script.UI
{
    public class UIManager : AbstractManager
    {
        public override void Initialize()
        {
            EventBus<UIOpenCloseEvent>.Event += OpenClose;
        }

        private void OnDestroy()
        {
            EventBus<UIOpenCloseEvent>.Event -= OpenClose;
        }

        public static void OpenCloseWindow(IWindow window)
            => EventBus<UIOpenCloseEvent>.Invoke(UIManagerEvents.UIOpenClose.Init(window));
        
        
        private IWindow _currentWindow;
        private void OpenClose(UIOpenCloseEvent evt)
        {
            if (_currentWindow != null)
            {
                _currentWindow.Close();
                _currentWindow = null;
            }

            if (evt.Window == null) return;
            
            _currentWindow = evt.Window;
            _currentWindow.Open();
        }
        
    }
}