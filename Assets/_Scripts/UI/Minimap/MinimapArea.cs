using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace _Scripts.UI.Minimap
{
    // 미니맵에 그릴 공간 하나.
    public class MinimapArea
    {
        // 지형으로 칠할 타일맵.
        public IReadOnlyList<Tilemap> Tilemaps { get; }

        // true면 플레이어가 지나간 주변만 밝히고(전장의 안개), false면 처음부터 전체를 보여준다.
        public bool UsesFog { get; }

        public MinimapArea(IReadOnlyList<Tilemap> tilemaps, bool usesFog)
        {
            Tilemaps = tilemaps;
            UsesFog = usesFog;
        }
    }
}
