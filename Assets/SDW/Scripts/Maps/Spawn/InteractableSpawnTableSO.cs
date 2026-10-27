using System.Collections.Generic;
using Lrw.Script.Agent.Players.InteractSystem;
using UnityEngine;

namespace SDW.Scripts.Maps.Spawn
{
    // 스폰 포인트 하나에서 무엇이 나올지 정하는 테이블.
    // 등장 확률을 먼저 굴리고, 성공하면 후보 중 가중치로 하나를 고른다.
    // 상자/NPC/상점을 한 테이블에 섞을 수 있어서, 새 오브젝트 종류는 프리팹만 후보에 추가하면 된다.
    [CreateAssetMenu(fileName = "Spawn_", menuName = "Map/Spawn Table")]
    public class InteractableSpawnTableSO : ScriptableObject
    {
        [Tooltip("이 포인트에 무엇이든 나올 확률 (0~1). 1이면 확정 등장.")]
        [SerializeField, Range(0f, 1f)] private float spawnChance = 1f;
        [Tooltip("후보와 가중치")]
        [SerializeField] private List<InteractableSpawnEntry> candidates = new();

        // 등장 확률에 실패했거나 유효한 후보가 없으면 false.
        public bool TryPick(out AbstractInteractObject prefab)
        {
            prefab = null;

            if (Random.value >= spawnChance)
                return false;

            prefab = PickByWeight();
            return prefab != null;
        }

        // 프리팹이 비어 있거나 가중치가 0인 후보는 제외하고, 가중치 비율로 하나를 고른다.
        private AbstractInteractObject PickByWeight()
        {
            float totalWeight = 0f;
            foreach (InteractableSpawnEntry entry in candidates)
            {
                if (entry != null && entry.Prefab != null)
                    totalWeight += entry.Weight;
            }

            if (totalWeight <= 0f)
                return null;

            float roll = Random.value * totalWeight;
            AbstractInteractObject last = null;

            foreach (InteractableSpawnEntry entry in candidates)
            {
                if (entry == null || entry.Prefab == null || entry.Weight <= 0f)
                    continue;

                last = entry.Prefab;
                roll -= entry.Weight;
                if (roll < 0f)
                    return last;
            }

            // 부동소수점 오차로 끝까지 남은 경우 마지막 유효 후보를 쓴다.
            return last;
        }
    }
}
