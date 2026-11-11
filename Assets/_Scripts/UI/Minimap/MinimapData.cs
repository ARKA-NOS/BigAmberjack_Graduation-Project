using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // 미니맵이 지금 방을 그리는 데 필요한 값. MinimapViewModel이 방마다 한 번 만들어 캐시한다.
    public class MinimapData
    {
        // 방 지형(충돌 타일)을 칠한 텍스처. WorldRect 전체에 늘려서 그린다.
        public Texture2D Terrain { get; }

        // 방 타일맵 전체를 감싸는 월드 범위.
        public Rect WorldRect { get; }

        public MinimapData(Texture2D terrain, Rect worldRect)
        {
            Terrain = terrain;
            WorldRect = worldRect;
        }
    }
}
