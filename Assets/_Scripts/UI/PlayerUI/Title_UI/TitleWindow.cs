using _Scripts.Agent.Players;
using _Scripts.UI.UI_System;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TitleWindow : AbstractWindow
    {
        [SerializeField] private PlayerInputSo inputSo;
        
        private CanvasGroup _canvasGroup;
        
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            inputSo.OnTitleKeyPressed += TitleOpenClose;
            Close();
        }

        private void OnDestroy()
        {
            inputSo.OnTitleKeyPressed -= TitleOpenClose;
        }

        public void TitleOpenClose()
        {
            UIManagerEvents.OpenCloseWindow(this);
        }

        public override void Open(object data)
        {
            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        public override void Close()
        {
           _canvasGroup.alpha = 0;
           _canvasGroup.interactable = false;
           _canvasGroup.blocksRaycasts = false;
        }
        
    }
}