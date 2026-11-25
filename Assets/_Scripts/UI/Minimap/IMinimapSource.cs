using System;
using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // 미니맵 ViewModel이 읽는 Model. 지금 공간의 지형과 플레이어를 감싸서, ViewModel이 맵 쪽 구체 클래스에 묶이지 않게 한다.
    public interface IMinimapSource
    {
        // 지금 그릴 공간. 그릴 지형이 없으면(맵 생성 중 등) null.
        MinimapArea CurrentArea { get; }

        // 플레이어 위치. 플레이어가 없으면 null.
        Transform Player { get; }

        // CurrentArea가 바뀔 때(새 맵 생성, 맵 제거) 호출된다. 씬 이동은 MinimapConnector가 다시 연결해서 처리한다.
        event Action AreaChanged;
    }
}
