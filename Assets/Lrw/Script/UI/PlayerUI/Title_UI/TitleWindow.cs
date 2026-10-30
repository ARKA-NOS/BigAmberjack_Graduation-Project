using System;
using Lrw.Script.Agent.Players;
using UnityEngine;

namespace Lrw.Script.UI.PlayerUI.Title_UI
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

        public void TitleOpenClose()
        {
            UIManagerEvents.OpenCloseWindow(this);
        }

        public override void Open(object data)
        {
            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
        }

        public override void Close()
        {
           _canvasGroup.alpha = 0;
           _canvasGroup.interactable = false;
        }
        
    }
}