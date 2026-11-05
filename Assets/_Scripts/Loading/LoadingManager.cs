using _Scripts._Core._EventSystem;
using _Scripts._Core._Manager;
using UnityEngine;

namespace _Scripts.Loading
{
    public class LoadingManager : AbstractManager
    {
        [SerializeField] private LoadingViewModel loadingViewModel;
        
        public override void Initialize()
        {
            EventBus<SetLoadingGaugeEvent>.Subscribe(HandleSetLoadingGauge);
        }

        private void OnDestroy()
        {
            EventBus<SetLoadingGaugeEvent>.UnSubscribe(HandleSetLoadingGauge);
        }

        private void HandleSetLoadingGauge(SetLoadingGaugeEvent evt)
        {
            if(loadingViewModel != null)
                loadingViewModel.Bind(evt.Value);
        }
        
    }
}