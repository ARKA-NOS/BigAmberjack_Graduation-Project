using UnityEngine;
using UnityEngine.Tilemaps;

namespace SDW.Scripts.Maps.Tiles
{
    /// <summary>
    /// 벽 RuleTile과 경사면 RuleTile이 서로를 이웃으로 인식하게 해 주는 지형 타일.
    /// 같은 타일맵에 칠한 LabTerrainTile끼리는 에셋이 달라도 하나의 지형으로 취급한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Tiles/Lab Terrain Tile")]
    public class LabTerrainTile : RuleTile<LabTerrainTile.Neighbor>
    {
        [field: SerializeField] public bool IsSlope { get; private set; }

        public class Neighbor : RuleTile.TilingRuleOutput.Neighbor
        {
            // This(1) = 지형(벽/경사 모두), NotThis(2) = 지형 아님(빈칸)
            public const int Slope = 3;
            public const int Wall = 4;
            public const int NotWall = 5;
        }

        public override bool RuleMatch(int neighbor, TileBase other)
        {
            if (other is RuleOverrideTile overrideTile)
                other = overrideTile.m_InstanceTile;

            var terrain = other as LabTerrainTile;
            switch (neighbor)
            {
                case Neighbor.This: return terrain != null;
                case Neighbor.NotThis: return terrain == null;
                case Neighbor.Slope: return terrain != null && terrain.IsSlope;
                case Neighbor.Wall: return terrain != null && !terrain.IsSlope;
                case Neighbor.NotWall: return terrain == null || terrain.IsSlope;
            }
            return true;
        }
    }
}
