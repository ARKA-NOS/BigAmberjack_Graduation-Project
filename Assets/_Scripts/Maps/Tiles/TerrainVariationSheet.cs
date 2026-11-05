using System;
using UnityEngine;

namespace SDW.Scripts.Maps.Tiles
{
    /// <summary>
    /// 타일셋 안의 한 블록(예: 8x8 벽 프레임)을 바리에이션 소스로 쓰기 위한 데이터.
    /// 가장자리(캡) 칸은 그대로 두고, 안쪽 칸들을 이음새가 자연스럽게 이어지는 순서로 골라 준다.
    /// 스프라이트 배열과 이음새 비용은 에디터에서 Bake 해 두고 런타임에는 읽기만 한다.
    /// </summary>
    [Serializable]
    public class TerrainVariationSheet
    {
        [Tooltip("블록이 있는 타일셋 텍스처")]
        [SerializeField] private Texture2D _texture;
        [Tooltip("텍스처 왼쪽 위 기준, 블록 시작 칸(타일 단위)")]
        [SerializeField] private Vector2Int _originCell;
        [Tooltip("블록 크기(타일 단위)")]
        [SerializeField] private Vector2Int _size = new(8, 8);
        [Tooltip("한 칸의 픽셀 크기")]
        [SerializeField] private Vector2Int _cellPixelSize = new(16, 16);
        [Tooltip("가장자리로 고정할 칸 수 (바리에이션에서 제외)")]
        [SerializeField] private int _capLeft = 2, _capRight = 2, _capTop = 2, _capBottom = 2;

        [Header("Baked (Bake Variation Sheets 로 생성)")]
        [SerializeField] private Sprite[] _sprites;    // row-major, row 0 = 맨 위
        [SerializeField] private float[] _horizontalCost; // [a * width + b] = 열 a 오른쪽에 열 b를 붙일 때의 이음새 비용
        [SerializeField] private float[] _verticalCost;   // [a * height + b] = 행 a 아래에 행 b를 붙일 때의 이음새 비용

        public int Width => _size.x;
        public int Height => _size.y;
        public int CapLeft => _capLeft;
        public int CapRight => _capRight;
        public int CapTop => _capTop;
        public int CapBottom => _capBottom;

        public bool IsBaked =>
            _sprites != null && _sprites.Length == Width * Height &&
            _horizontalCost != null && _horizontalCost.Length == Width * Width &&
            _verticalCost != null && _verticalCost.Length == Height * Height;

        public Sprite GetSprite(int col, int row) => _sprites[row * Width + col];
        public Sprite[] Sprites => _sprites;

        public bool IsInteriorColumn(int col) => col >= _capLeft && col < Width - _capRight;
        public bool IsInteriorRow(int row) => row >= _capTop && row < Height - _capBottom;

        #region Path Selection

        private static float[] _dpCost = new float[256];
        private static int[] _dpPrev = new int[256];

        /// <summary>
        /// 안쪽 칸 count개를 채울 열(또는 행) 순서를 고르고, 그중 index번째 값을 돌려준다.
        /// 이음새 비용 + 해시 노이즈의 합이 최소가 되는 경로(Viterbi)를 고르므로
        /// 원본 아트에서 이어지던 순서는 최대한 유지하고, variation 만큼만 섞는다.
        /// </summary>
        public int PickInterior(bool horizontal, int count, int index, float variation, uint key)
        {
            int size = horizontal ? Width : Height;
            int first = horizontal ? _capLeft : _capTop;
            int last = size - (horizontal ? _capRight : _capBottom) - 1;
            float[] cost = horizontal ? _horizontalCost : _verticalCost;
            int nodes = last - first + 1;
            if (nodes <= 0 || count <= 0) return -1;
            if (nodes == 1) return first;

            int total = count * nodes;
            if (_dpCost.Length < total)
            {
                _dpCost = new float[total * 2];
                _dpPrev = new int[total * 2];
            }

            for (int j = 0; j < nodes; j++)
            {
                int v = first + j;
                float start = first > 0 ? cost[(first - 1) * size + v] : 0f;
                _dpCost[j] = start + variation * Hash01(key, 0, v);
            }

            for (int i = 1; i < count; i++)
            {
                for (int j = 0; j < nodes; j++)
                {
                    int v = first + j;
                    float best = float.MaxValue;
                    int bestK = 0;
                    for (int k = 0; k < nodes; k++)
                    {
                        float c = _dpCost[(i - 1) * nodes + k] + cost[(first + k) * size + v];
                        if (c < best)
                        {
                            best = c;
                            bestK = k;
                        }
                    }
                    _dpCost[i * nodes + j] = best + variation * Hash01(key, i, v);
                    _dpPrev[i * nodes + j] = bestK;
                }
            }

            float bestEnd = float.MaxValue;
            int node = 0;
            for (int j = 0; j < nodes; j++)
            {
                int v = first + j;
                float end = last + 1 < size ? cost[v * size + last + 1] : 0f;
                float c = _dpCost[(count - 1) * nodes + j] + end;
                if (c < bestEnd)
                {
                    bestEnd = c;
                    node = j;
                }
            }

            for (int i = count - 1; i > index; i--)
                node = _dpPrev[i * nodes + node];

            return first + node;
        }

        private static float Hash01(uint key, int i, int v)
        {
            uint h = key;
            h = Mix(h ^ (uint)i);
            h = Mix(h ^ (uint)v);
            return (h & 0xFFFFFF) / (float)0x1000000;
        }

        public static uint Mix(uint h)
        {
            h ^= h >> 16;
            h *= 0x7FEB352D;
            h ^= h >> 15;
            h *= 0x846CA68B;
            h ^= h >> 16;
            return h;
        }

        #endregion

#if UNITY_EDITOR
        #region Bake

        /// <summary>
        /// 텍스처에서 블록 스프라이트를 찾고, 칸끼리 붙였을 때의 이음새 비용을 픽셀로 계산한다.
        /// 비용 = B의 왼쪽 열이 'A의 원래 오른쪽 이웃'의 왼쪽 열과 다른 픽셀 수
        ///      + A의 오른쪽 열이 'B의 원래 왼쪽 이웃'의 오른쪽 열과 다른 픽셀 수.
        /// 원본에서 이웃이던 쌍은 0이 되고, 모양이 이어지지 않을수록 커진다.
        /// </summary>
        public string Bake()
        {
            if (_texture == null) return "텍스처가 비어 있음";

            string path = UnityEditor.AssetDatabase.GetAssetPath(_texture);
            var pixels = new Texture2D(2, 2);
            if (!pixels.LoadImage(System.IO.File.ReadAllBytes(path)))
                return $"이미지를 읽을 수 없음: {path}";

            int texH = pixels.height;
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            _sprites = new Sprite[Width * Height];
            for (int r = 0; r < Height; r++)
            for (int c = 0; c < Width; c++)
            {
                float x = (_originCell.x + c) * _cellPixelSize.x;
                float y = texH - (_originCell.y + r + 1) * _cellPixelSize.y;
                foreach (var obj in all)
                {
                    if (obj is Sprite s && Mathf.Approximately(s.rect.x, x) && Mathf.Approximately(s.rect.y, y))
                    {
                        _sprites[r * Width + c] = s;
                        break;
                    }
                }
                if (_sprites[r * Width + c] == null)
                    return $"({c},{r}) 칸의 스프라이트를 찾지 못함";
            }

            Color32[] px = pixels.GetPixels32();
            int texW = pixels.width;
            Color32 Pixel(int col, int row, int lx, int ly) // ly: 칸 위쪽 기준
            {
                int gx = (_originCell.x + col) * _cellPixelSize.x + lx;
                int gy = texH - 1 - ((_originCell.y + row) * _cellPixelSize.y + ly);
                return px[gy * texW + gx];
            }

            int ColumnDiff(int row, int colA, int lxA, int colB, int lxB)
            {
                int diff = 0;
                for (int ly = 0; ly < _cellPixelSize.y; ly++)
                    if (!Pixel(colA, row, lxA, ly).Equals(Pixel(colB, row, lxB, ly))) diff++;
                return diff;
            }

            int RowDiff(int col, int rowA, int lyA, int rowB, int lyB)
            {
                int diff = 0;
                for (int lx = 0; lx < _cellPixelSize.x; lx++)
                    if (!Pixel(col, rowA, lx, lyA).Equals(Pixel(col, rowB, lx, lyB))) diff++;
                return diff;
            }

            int lastX = _cellPixelSize.x - 1, lastY = _cellPixelSize.y - 1;

            _horizontalCost = new float[Width * Width];
            for (int a = 0; a < Width; a++)
            for (int b = 0; b < Width; b++)
            {
                int sum = 0;
                for (int r = 0; r < Height; r++)
                {
                    if (a + 1 < Width && b - 1 >= 0)
                        sum += ColumnDiff(r, b, 0, a + 1, 0) + ColumnDiff(r, a, lastX, b - 1, lastX);
                    else
                        sum += 2 * ColumnDiff(r, a, lastX, b, 0);
                }
                _horizontalCost[a * Width + b] = sum;
            }

            _verticalCost = new float[Height * Height];
            for (int a = 0; a < Height; a++)
            for (int b = 0; b < Height; b++)
            {
                int sum = 0;
                for (int c = 0; c < Width; c++)
                {
                    if (a + 1 < Height && b - 1 >= 0)
                        sum += RowDiff(c, b, 0, a + 1, 0) + RowDiff(c, a, lastY, b - 1, lastY);
                    else
                        sum += 2 * RowDiff(c, a, lastY, b, 0);
                }
                _verticalCost[a * Height + b] = sum;
            }

            UnityEngine.Object.DestroyImmediate(pixels);
            return null;
        }

        #endregion
#endif
    }
}
