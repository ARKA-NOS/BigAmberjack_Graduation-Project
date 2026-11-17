using _Scripts._Core._EventSystem;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies;
using _Scripts.UI.RunEnd;
using UnityEngine;

namespace _Scripts.Currency
{
    [DisallowMultipleComponent]
    public class DeathCurrencyRewarder : MonoBehaviour
    {
        [SerializeField] private CurrencySO rewardCurrency;
        [SerializeField] private int baseReward = 20;
        [SerializeField] private int rewardPerKill = 2;

        private bool _rewarded;

        private void OnEnable() => EventBus<RunEndedEvent>.Subscribe(HandleRunEnded);
        private void OnDisable() => EventBus<RunEndedEvent>.UnSubscribe(HandleRunEnded);

        private void HandleRunEnded(RunEndedEvent evt)
        {
            if (_rewarded || evt.Reason != RunEndReason.PlayerDead) return;
            _rewarded = true;

            int killCount = GetKillCount();
            int amount = CalculateReward(killCount);
            CurrencyData data = rewardCurrency != null ? CurrencySystem.GetCurrencyData(rewardCurrency) : null;
            if (data == null)
            {
                Debug.LogWarning($"[{name}] 보상 재화를 찾지 못해 사망 보상을 지급하지 못했습니다.", this);
                return;
            }

            data.Add(amount);
            Debug.Log($"[{name}] 사망 보상 {rewardCurrency.CurrencyName} +{amount} (처치 {killCount})", this);
        }

        public int CalculateReward(int killCount) => baseReward + killCount * rewardPerKill;

        private static int GetKillCount()
        {
            EnemyKillCountManager manager = ServiceLocator.Get<EnemyKillCountManager>();
            return manager != null ? manager.GetKillCount() : 0;
        }

        [ContextMenu("Debug/Simulate Player Death Reward")]
        private void SimulateDeathReward() => RunEndEvents.RaiseRunEnded(RunEndReason.PlayerDead);

        [ContextMenu("Debug/Log Expected Reward")]
        private void LogExpectedReward()
        {
            int killCount = GetKillCount();
            Debug.Log($"처치 {killCount} → 예상 보상 {CalculateReward(killCount)}", this);
        }
    }
}
