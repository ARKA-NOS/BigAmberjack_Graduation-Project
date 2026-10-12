using System;
using System.Collections;
using CTJ.Enemies;
using UnityEngine;

namespace SDW.Scripts.Maps.Conditions
{
    // 방에서 스폰된 적이 전부 사라지면 클리어된다.
    public class KillAllEnemiesCondition : IRoomClearCondition
    {
        private const float ClearCheckInterval = 0.2f;

        private readonly RoomController _room;
        private Coroutine _routine;

        public event Action Cleared;

        public KillAllEnemiesCondition(RoomController room)
        {
            _room = room;
        }

        public void Begin()
        {
            _routine = _room.StartCoroutine(WaitForAllEnemiesDead());
        }

        public void Dispose()
        {
            if (_routine != null && _room != null)
                _room.StopCoroutine(_routine);

            _routine = null;
        }

        // 적 사망 이벤트가 따로 없어서, 스폰한 적들이 전부 파괴됐는지 주기적으로 확인한다.
        private IEnumerator WaitForAllEnemiesDead()
        {
            WaitForSeconds interval = new WaitForSeconds(ClearCheckInterval);

            while (HasAliveEnemy())
                yield return interval;

            _routine = null;
            Cleared?.Invoke();
        }

        private bool HasAliveEnemy()
        {
            foreach (EnemyBase enemy in _room.SpawnedEnemies)
            {
                if (enemy != null)
                    return true;
            }

            return false;
        }
    }
}
