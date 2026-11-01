using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SDW.Scripts.Maps.Tiles
{
    /// <summary>
    /// 벽 RuleTile과 경사면 RuleTile이 서로를 이웃으로 인식하게 해 주는 지형 타일.
    /// 같은 타일맵에 칠한 LabTerrainTile끼리는 에셋이 달라도 하나의 지형으로 취급한다.
    /// 룰이 고른 스프라이트가 바리에이션 시트의 안쪽 칸이면, 지형 줄(가로/세로) 전체를 보고
    /// 이음새가 이어지는 바리에이션으로 바꿔 끼운다.
    /// </summary>
    [CreateAssetMenu(menuName = "Tiles/Lab Terrain Tile")]
    public class LabTerrainTile : RuleTile<LabTerrainTile.Neighbor>
    {
        private const int MaxScan = 128;

        [field: SerializeField] public bool IsSlope { get; private set; }

        [Header("Variation")]
        [Tooltip("룰 출력 스프라이트가 이 시트들의 안쪽 칸이면 바리에이션으로 바꾼다")]
        [SerializeField] private List<TerrainVariationSheet> _variationSheets = new();
        [Tooltip("0 = 원본 아트 순서를 최대한 유지(이음새 최소), 클수록 더 섞인다. 단위는 이음새 불일치 픽셀 수")]
        [SerializeField, Range(0f, 60f)] private float _variation = 12f;
        [SerializeField] private int _variationSeed;

        private Dictionary<Sprite, (TerrainVariationSheet sheet, int col, int row)> _spriteLookup;

        public class Neighbor : RuleTile.TilingRuleOutput.Neighbor
        {
            // This(1) = 지형(벽/경사 모두), NotThis(2) = 지형 아님(빈칸)
            public const int Slope = 3;
            public const int Wall = 4;
            public const int NotWall = 5;
        }

        public override bool RuleMatch(int neighbor, TileBase other)
        {
            var terrain = AsTerrain(other);
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

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            base.GetTileData(position, tilemap, ref tileData);

            if (tileData.sprite == null || !TryGetSheetCell(tileData.sprite, out var sheet, out int col, out int row))
                return;

            if (sheet.IsInteriorColumn(col))
            {
                int picked = PickAlongRun(sheet, true, position, tilemap);
                if (picked >= 0) col = picked;
            }
            if (sheet.IsInteriorRow(row))
            {
                int picked = PickAlongRun(sheet, false, position, tilemap);
                if (picked >= 0) row = picked;
            }

            tileData.sprite = sheet.GetSprite(col, row);
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            base.RefreshTile(position, tilemap);

            if (_variationSheets.Count == 0) return;

            // 바리에이션은 줄 전체 길이로 결정되므로, 칠하거나 지울 때 같은 가로/세로 줄을 모두 다시 그린다.
            RefreshRun(position, Vector3Int.left, tilemap);
            RefreshRun(position, Vector3Int.right, tilemap);
            RefreshRun(position, Vector3Int.up, tilemap);
            RefreshRun(position, Vector3Int.down, tilemap);
        }

        private static void RefreshRun(Vector3Int from, Vector3Int step, ITilemap tilemap)
        {
            for (int i = 1; i <= MaxScan; i++)
            {
                var p = from + step * i;
                if (AsTerrain(tilemap.GetTile(p)) == null) return;
                tilemap.RefreshTile(p);
            }
        }

        /// <summary>
        /// position이 속한 지형 줄(가로면 왼→오, 세로면 위→아래)의 안쪽 칸 중 몇 번째인지 구하고
        /// 그 줄 전체에 맞춰 고른 시트 열/행 번호를 돌려준다. 안쪽 칸이 아니면 -1.
        /// 같은 위치에서 시작하고 길이가 같은 줄은 같은 순서가 나오므로, 사각형 지형은 위아래 줄이 정렬된다.
        /// </summary>
        private int PickAlongRun(TerrainVariationSheet sheet, bool horizontal, Vector3Int position, ITilemap tilemap)
        {
            var forward = horizontal ? Vector3Int.right : Vector3Int.down;

            int before = 0;
            while (before < MaxScan && AsTerrain(tilemap.GetTile(position - forward * (before + 1))) != null)
                before++;
            int after = 0;
            while (after < MaxScan && AsTerrain(tilemap.GetTile(position + forward * (after + 1))) != null)
                after++;

            int capFirst = horizontal ? sheet.CapLeft : sheet.CapTop;
            int capLast = horizontal ? sheet.CapRight : sheet.CapBottom;
            int count = before + after + 1 - capFirst - capLast;
            int index = before - capFirst;
            if (count <= 0 || index < 0 || index >= count) return -1;

            var start = position - forward * before;
            uint key = TerrainVariationSheet.Mix((uint)_variationSeed * 0x9E3779B9u ^ (horizontal ? 0x68E31DA4u : 0xB5297A4Du));
            key = TerrainVariationSheet.Mix(key ^ (uint)(horizontal ? start.x : start.y));
            key = TerrainVariationSheet.Mix(key ^ (uint)count);

            return sheet.PickInterior(horizontal, count, index, _variation, key);
        }

        private bool TryGetSheetCell(Sprite sprite, out TerrainVariationSheet sheet, out int col, out int row)
        {
            if (_spriteLookup == null) BuildLookup();

            if (_spriteLookup.TryGetValue(sprite, out var cell))
            {
                (sheet, col, row) = cell;
                return true;
            }
            sheet = null;
            col = row = -1;
            return false;
        }

        private void BuildLookup()
        {
            _spriteLookup = new Dictionary<Sprite, (TerrainVariationSheet, int, int)>();
            foreach (var sheet in _variationSheets)
            {
                if (sheet == null || !sheet.IsBaked) continue;
                for (int r = 0; r < sheet.Height; r++)
                for (int c = 0; c < sheet.Width; c++)
                {
                    var s = sheet.GetSprite(c, r);
                    if (s != null) _spriteLookup.TryAdd(s, (sheet, c, r));
                }
            }
        }

        private static LabTerrainTile AsTerrain(TileBase tile)
        {
            if (tile is RuleOverrideTile overrideTile)
                tile = overrideTile.m_InstanceTile;
            return tile as LabTerrainTile;
        }

        private void OnValidate() => _spriteLookup = null;

#if UNITY_EDITOR
        [ContextMenu("Bake Variation Sheets")]
        private void BakeVariationSheets()
        {
            UnityEditor.Undo.RecordObject(this, "Bake Variation Sheets");
            for (int i = 0; i < _variationSheets.Count; i++)
            {
                string error = _variationSheets[i].Bake();
                if (error != null) Debug.LogError($"[{name}] 바리에이션 시트 {i} Bake 실패: {error}", this);
            }
            _spriteLookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[{name}] 바리에이션 시트 {_variationSheets.Count}개 Bake 완료", this);
        }
#endif
    }
}
