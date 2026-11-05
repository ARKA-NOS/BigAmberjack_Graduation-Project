using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Maps.Encounter
{
    // 전투 구역에서 나올 적 구성. 웨이브가 하나면 단일 스폰, 여러 개면 웨이브형 스폰이다.
    // 위치(스폰 포인트, 배리어)는 방 프리팹의 EncounterZone이 가지고, 이 SO는 무엇이 나올지만 정한다.
    [CreateAssetMenu(fileName = "Encounter_", menuName = "Map/Encounter")]
    public class EncounterSO : ScriptableObject
    {
        [SerializeField] private List<EncounterWave> waves = new();
        [Tooltip("적 수가 스폰 포인트보다 많아 같은 포인트를 다시 쓸 때, 좌우로 띄워 생성하는 간격")]
        [SerializeField, Min(0f)] private float sameSpawnPointSpacing = 1.5f;

        public IReadOnlyList<EncounterWave> Waves => waves;
        public float SameSpawnPointSpacing => sameSpawnPointSpacing;

        // 웨이브가 둘 이상이면 웨이브형. 웨이브형 전투는 끝날 때까지 주변 상호작용을 잠근다.
        public bool IsWave => waves.Count > 1;
    }
}
