using Lrw.Script._Core._EventSystem;
using Lrw.Script._Core._Manager;

namespace Lrw.Script.UI
{
    public class UIManager : AbstractManager
    {
        public override void Initialize()
        {
            EventBus<UIOpenCloseEvent>.Subscribe(OpenClose);
        }
        
        public override void GameEnd()
        {
            base.GameEnd();
            EventBus<UIOpenCloseEvent>.UnSubscribe(OpenClose);
        }
        
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
            _currentWindow.Open(evt.Data);
        }
        
        
    }
}