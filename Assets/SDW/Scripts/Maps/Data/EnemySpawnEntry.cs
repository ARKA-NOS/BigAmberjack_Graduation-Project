using System;
using CTJ.Enemies;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
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
