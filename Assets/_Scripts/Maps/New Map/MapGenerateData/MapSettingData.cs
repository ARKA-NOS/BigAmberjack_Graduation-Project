using _Scripts.Maps.New_Map.MapSettingAsset;
using UnityEngine;

namespace _Scripts.Maps.New_Map.MapGenerateData
{
    // 스레드에 넘기는 생성 설정 사본. 메인 스레드에서 MapGenerateSetting을 읽어 만든다.
    public readonly struct MapSettingData
    {
        public readonly int Seed;
        public readonly int MainCombatCount;
        public readonly Vector2Int BranchAnchorRange;
        public readonly int ShopBranchCount;
        public readonly int RewardBranchCount;
        public readonly int RewardBranchCombatCount;
        public readonly int MaxPlacementSteps;
        public readonly int MaxFullRetries;
        public readonly Vector2Int MapBounds;

        public MapSettingData(MapGenerateSetting setting, int seed)
        {
            Seed = seed;
            MainCombatCount = Mathf.Max(0, setting.MainCombatCount);
            BranchAnchorRange = setting.BranchAnchorRange;
            ShopBranchCount = Mathf.Max(0, setting.ShopBranchCount);
            RewardBranchCount = Mathf.Max(0, setting.RewardBranchCount);
            RewardBranchCombatCount = Mathf.Max(0, setting.RewardBranchCombatCount);
            MaxPlacementSteps = Mathf.Max(1, setting.MaxPlacementSteps);
            MaxFullRetries = Mathf.Max(1, setting.MaxFullRetries);
            MapBounds = setting.MapBounds;
        }

        // 그래프의 전체 방 수: 시작 + 전투 + 출구 + 상점 분기 + 보상 분기(추가 전투 + 보상)
        public int TotalRoomCount
            => 2 + MainCombatCount + ShopBranchCount + RewardBranchCount * (RewardBranchCombatCount + 1);
    }
}
