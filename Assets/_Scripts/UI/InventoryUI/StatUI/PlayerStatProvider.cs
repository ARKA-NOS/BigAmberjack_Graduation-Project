using System.Collections.Generic;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players;
using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.UI.InventoryUI.StatUI
{
    // 현재 플레이어의 스탯을 ViewModel에 넘긴다. 플레이어 프리팹은 건드리지 않고 ServiceLocator로 찾는다.
    // 플레이어는 던전 생성 시 나중에 생기거나 씬 전환 때 다시 만들어지므로, 연결이 끊기면 다시 찾는다.
    public class PlayerStatProvider : MonoBehaviour
    {
        [SerializeField] private StatListViewModel viewModel;

        private StatModule _statModule;
        private readonly List<Stat> _subscribed = new();

        private void Update()
        {
            if (_statModule != null) return;

            Unsubscribe();
            if (ServiceLocator.Get<IPlayerController>() is not ModuleOwner player || player == null) return;
            if (!player.TryGetModule(out _statModule)) return;

            // GetStats는 IStatModule에 없어서 StatModule을 직접 쓴다.
            foreach (Stat stat in _statModule.GetStats().Values)
            {
                stat.OnValueChanged += HandleValueChanged;
                _subscribed.Add(stat);
            }
            BindViewModel();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void HandleValueChanged(float newValue, float delta)
        {
            BindViewModel();
        }

        private void BindViewModel()
        {
            if (viewModel != null && _statModule != null)
                viewModel.Bind(_statModule.GetStats());
        }

        private void Unsubscribe()
        {
            foreach (Stat stat in _subscribed)
                stat.OnValueChanged -= HandleValueChanged;
            _subscribed.Clear();
        }
    }
}
