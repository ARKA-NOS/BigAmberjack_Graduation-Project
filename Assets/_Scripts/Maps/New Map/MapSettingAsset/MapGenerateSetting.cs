using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Maps.New_Map.MapSettingAsset
{
    [CreateAssetMenu(fileName = "Map Create Setting", menuName = "MapSystem/Map Create Setting", order = 0)]
    public class MapGenerateSetting : ScriptableObject
    {
        [field: SerializeField] public int Seed { get; private set; }

        [field: Header("Room Settings")]
        // Start 역할 방이 없을 때 쓰는 시작방
        [field: SerializeField] public RoomSO DefaultRoom { get; private set; }
        // 모든 역할의 방. 이 목록 순서대로 굽고, 같은 에셋이 여러 번 있으면 처음 것만 쓴다.
        [field: SerializeField] public List<RoomSO> Rooms { get; private set; }

        [field: Header("Graph")]
        [field: SerializeField] public int MainCombatCount { get; private set; } = 5;
        // 분기를 붙일 주 경로 전투방 번호(1부터, 양 끝 포함)
        [field: SerializeField] public Vector2Int BranchAnchorRange { get; private set; } = new(2, 4);
        [field: SerializeField] public int ShopBranchCount { get; private set; } = 1;
        [field: SerializeField] public int RewardBranchCount { get; private set; } = 1;
        // 보상 분기에서 보상방 앞에 놓는 추가 전투방 수
        [field: SerializeField] public int RewardBranchCombatCount { get; private set; } = 1;

        [field: Header("Limits")]
        // 한 번의 배치 시도에서 검사할 후보 수
        [field: SerializeField] public int MaxPlacementSteps { get; private set; } = 4000;
        // 그래프부터 다시 만드는 전체 생성 시도 횟수. 모두 실패하면 기본 맵을 쓴다.
        [field: SerializeField] public int MaxFullRetries { get; private set; } = 5;
        // 시작방 원점 중심의 배치 범위(타일 칸). x, y 중 하나라도 0 이하이면 제한하지 않는다.
        [field: SerializeField] public Vector2Int MapBounds { get; private set; } = new(100, 100);
        // 미리 만든 기본 맵
        [field: SerializeField] public GameObject FallbackMapPrefab { get; private set; }
    }
}
