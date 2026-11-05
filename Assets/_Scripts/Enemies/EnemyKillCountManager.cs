using Lrw.Script._Core._Manager;
using Lrw.Script._Core._ServiceLocator;
using UnityEngine;

namespace CTJ.Enemies
{
    // 지금은 독립 오브젝트로 사용하고, 이후 GameManager 자식으로 옮겨도 같은 초기화 경로를 사용합니다.
    [DefaultExecutionOrder(-10), DisallowMultipleComponent]
    public sealed class EnemyKillCountManager : AbstractManager
    {
        private int _killCount;

        private void OnEnable() => Initialize();

        public override void Initialize()
        {
            if (!isActiveAndEnabled) return;
            EnemyKillCountManager current = ServiceLocator.Get<EnemyKillCountManager>();
            if (current != null && current != this && current.isActiveAndEnabled)
            {
                Debug.LogWarning("EnemyKillCountManager는 하나만 활성화해야 합니다. 중복 컴포넌트를 비활성화합니다.", this);
                enabled = false;
                return;
            }

            // GameManager.Initialize와 OnEnable이 모두 호출되어도 기존 처치 수는 유지합니다.
            ServiceLocator.Register(this);
        }

        public int GetKillCount() => _killCount;

        [ContextMenu("Debug/Reset Kill Count")]
        public void ResetKillCount() => _killCount = 0;

        internal void RegisterDeath()
        {
            if (isActiveAndEnabled && _killCount < int.MaxValue) _killCount++;
        }

        [ContextMenu("Debug/Log Kill Count")]
        private void LogKillCount() => Debug.Log($"총 적 처치 수: {GetKillCount()}", this);

        public override void GameEnd() => Unregister();
        private void OnDisable() => Unregister();

        private void Unregister()
        {
            // 먼저 등록된 매니저를 중복 객체의 종료가 해제하지 않도록 소유권을 확인합니다.
            if (ServiceLocator.Get<EnemyKillCountManager>() == this)
                ServiceLocator.Register<EnemyKillCountManager>(null);
        }
    }
}
