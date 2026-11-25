using _Scripts._Core._Debug;
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
        [SerializeField] private int baseReward = 10;
        [SerializeField] private int rewardPerKill = 2;
        [Tooltip("이 수보다 적게 처치하면 기본 보상까지 전부 지급하지 않는다. 바로 포기·사망해 보상만 받는 것을 막는다.")]
        [SerializeField] private int minKillsForReward = 1;

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
                FDebug.LogWarning($"[{name}] 보상 재화를 찾지 못해 사망 보상을 지급하지 못했습니다.", this);
                return;
            }

            data.Add(amount);
            FDebug.Log($"[{name}] 사망 보상 {rewardCurrency.CurrencyName} +{amount} (처치 {killCount})", this);
        }

        public int CalculateReward(int killCount)
            => killCount < minKillsForReward ? 0 : baseReward + killCount * rewardPerKill;

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
            FDebug.Log($"처치 {killCount} → 예상 보상 {CalculateReward(killCount)}", this);
        }
    }
}
