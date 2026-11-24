using System;
using _Scripts.Agent.Players.InteractSystem;
using UnityEngine;

namespace _Scripts.Maps.Spawn
{
    // 스폰 테이블의 후보 하나. 상자, NPC, 상점 등 상호작용 오브젝트 프리팹이면 무엇이든 넣을 수 있다.
    [Serializable]
    public class InteractableSpawnEntry
    {
        [SerializeField] private AbstractInteractObject prefab;
        [Tooltip("뽑힐 가중치. 후보 전체 가중치 합 대비 비율로 뽑힌다. 0이면 뽑히지 않는다.")]
        [SerializeField, Min(0f)] private float weight = 1f;

        [Tooltip("켜면 한 런에 한 번만 나온다. 한 번 뽑히면 그 런이 끝날 때까지 후보에서 빠진다(칭호 NPC 등).")]
        [SerializeField] private bool uniquePerRun;

        public AbstractInteractObject Prefab => prefab;
        public float Weight => weight;
        public bool UniquePerRun => uniquePerRun;
    }
}
