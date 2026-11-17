using System;
using _Scripts.Agent.Enemies;
using UnityEngine;

namespace _Scripts.Maps.Data
{
    [Serializable]
    public class EnemySpawnEntry
    {
        [SerializeField] private EnemyBase enemyPrefab;
        [SerializeField, Min(1)] private int count = 1;

        public EnemyBase EnemyPrefab => enemyPrefab;
        public int Count => count;
    }
}
