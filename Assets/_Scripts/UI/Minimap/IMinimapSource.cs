using System;
using _Scripts.Maps;
using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // 미니맵 ViewModel이 읽는 Model. 스테이지 생성기와 플레이어를 감싸서, ViewModel이 구체 클래스에 묶이지 않게 한다.
    public interface IMinimapSource
    {
        // 지금 플레이어가 있는 방. 아직 방에 들어가지 않았으면 null.
        RoomController CurrentRoom { get; }

        // 플레이어 위치. 플레이어가 없으면 null.
        Transform Player { get; }

        // 어느 방이든 들어올 때마다(첫 방 포함) 호출된다.
        event Action<RoomController> RoomEntered;
    }
}
