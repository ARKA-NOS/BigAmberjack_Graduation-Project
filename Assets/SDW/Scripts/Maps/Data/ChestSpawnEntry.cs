using System;
using SDW.Scripts.Interact.Chest;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 방의 보물상자 후보 하나. 상자가 나오기로 정해지면 weight 비율로 후보 중 하나를 고른다.
    [Serializable]
    public class ChestSpawnEntry
    {
        [SerializeField] private Chest chestPrefab;
        [Tooltip("뽑힐 가중치. 후보 전체 가중치 합 대비 비율로 뽑힌다. 0이면 뽑히지 않는다.")]
        [SerializeField, Min(0f)] private float weight = 1f;

        public Chest ChestPrefab => chestPrefab;
        public float Weight => weight;
    }
}
