using _Scripts.UI.Template;
using Lean.Pool;
using UnityEngine;

namespace _Scripts.Loading
{
    public class LoadingEventSubscribe : EventListener<SetLoadingGaugeEvent>
    {
        [SerializeField] private LoadingViewModel loadingViewModel;
        
        protected override void HandleEvent(SetLoadingGaugeEvent evt)
        {
            if (loadingViewModel == null) return;
            loadingViewModel.Bind(evt.Value);
        }
        
    }
}