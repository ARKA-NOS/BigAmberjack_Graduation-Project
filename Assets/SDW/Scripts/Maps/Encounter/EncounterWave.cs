using System;
using System.Collections.Generic;
using SDW.Scripts.Maps.Data;
using UnityEngine;

namespace SDW.Scripts.Maps.Encounter
{
    // 전투 구역의 웨이브 하나. 이전 웨이브의 적이 모두 죽은 뒤 startDelay만큼 기다렸다가 생성된다.
    [Serializable]
    public class EncounterWave
    {
        [Tooltip("이 웨이브가 시작되기 전 대기 시간(초). 첫 웨이브는 구역에 들어온 순간부터 잰다.")]
        [SerializeField, Min(0f)] private float startDelay;
        [SerializeField] private List<EnemySpawnEntry> enemies = new();

        public float StartDelay => startDelay;
        public IReadOnlyList<EnemySpawnEntry> Enemies => enemies;
    }
}
