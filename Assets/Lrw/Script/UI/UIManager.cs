using Lrw.Script._Core._EventSystem;
using Lrw.Script._Core._Manager;
using Lrw.Script.Agent.Players;
using UnityEngine;

namespace Lrw.Script.UI
{
    public class UIManager : AbstractManager
    {
        [SerializeField] private PlayerInputSo inputSo;
        
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
            IWindow prevWindow = _currentWindow;
            if (_currentWindow != null)
            {
                inputSo.SetPlayerInputActive(true);
                _currentWindow.Close();
                _currentWindow = null;
            }
            
            if (evt.Window == null || evt.Window == prevWindow) return;
            
            inputSo.SetPlayerInputActive(false);
            _currentWindow = evt.Window;
            _currentWindow.Open(evt.Data);
        }
        
        
    }
}