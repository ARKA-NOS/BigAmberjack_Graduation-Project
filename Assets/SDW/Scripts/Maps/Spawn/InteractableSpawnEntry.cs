using System;
using Lrw.Script.Agent.Players.InteractSystem;
using UnityEngine;

namespace SDW.Scripts.Maps.Spawn
{
    // 스폰 테이블의 후보 하나. 상자, NPC, 상점 등 상호작용 오브젝트 프리팹이면 무엇이든 넣을 수 있다.
    [Serializable]
    public class InteractableSpawnEntry
    {
        [SerializeField] private AbstractInteractObject prefab;
        [Tooltip("뽑힐 가중치. 후보 전체 가중치 합 대비 비율로 뽑힌다. 0이면 뽑히지 않는다.")]
        [SerializeField, Min(0f)] private float weight = 1f;

        public AbstractInteractObject Prefab => prefab;
        public float Weight => weight;
    }
}
