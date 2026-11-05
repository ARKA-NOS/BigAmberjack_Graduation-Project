using System;
using Lrw.Script._Core._EventSystem;
using Lrw.Script._Core._Manager;
using Lrw.Script.UI.BindSystem.ViewModel;
using UnityEngine;

namespace Lrw.Script.Loading
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