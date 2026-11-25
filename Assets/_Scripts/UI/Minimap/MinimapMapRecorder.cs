using System;
using System.Collections.Generic;
using _Scripts._Core._EventSystem;
using _Scripts.Maps.New_Map;
using _Scripts.Maps.New_Map.MapGenerateData;
using _Scripts.Maps.New_Map.MapSettingAsset;
using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // 마지막으로 만들어진 맵을 기억한다.
    // 미니맵은 PlayerPack 아래에 있어 첫 맵에서는 생성 완료 이벤트를 처리하는 도중에 생기므로, 직접 구독하면 그 이벤트를 놓친다.
    // 게임 시작 전에 구독해 두어, 맵 쪽 코드를 고치지 않고도 늦게 생긴 미니맵이 현재 맵을 알 수 있게 한다.
    public static class MinimapMapRecorder
    {
        private static readonly List<Room> Rooms = new();

        // 일반 맵의 방 목록. 씬이 바뀌었으면 파괴된 항목이 남을 수 있으므로 쓰는 쪽에서 null을 거른다.
        public static IReadOnlyList<Room> GeneratedRooms => Rooms;

        // 마지막 완료 이벤트가 보스 맵의 것인지. 보스 맵은 방 목록 없이 이벤트를 낸다.
        public static bool IsBossMap { get; private set; }

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Rooms.Clear();
            IsBossMap = false;
            Changed = null;

            // 도메인 리로드를 끄고 플레이를 다시 시작해도 중복 구독되지 않게 먼저 해제한다.
            EventBus<MapGenerateEndEvent>.UnSubscribe(HandleMapGenerated);
            EventBus<MapGenerateEndEvent>.Subscribe(HandleMapGenerated);
            EventBus<MapGenerateEvent>.UnSubscribe(HandleMapReplaced);
            EventBus<MapGenerateEvent>.Subscribe(HandleMapReplaced);
        }

        private static void HandleMapGenerated(MapGenerateEndEvent evt)
        {
            Rooms.Clear();
            IsBossMap = evt.Rooms == null;

            if (evt.Rooms != null)
            {
                foreach (GeneratedRoom room in evt.Rooms)
                    Rooms.Add(room.Instance);
            }

            Changed?.Invoke();
        }

        // 일반 맵을 다시 만들거나 지우기 직전에 온다.
        private static void HandleMapReplaced(MapGenerateEvent evt)
        {
            Rooms.Clear();
            IsBossMap = false;
            Changed?.Invoke();
        }
    }
}
