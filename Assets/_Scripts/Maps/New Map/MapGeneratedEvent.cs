using System.Collections.Generic;
using _Scripts._Core._EventSystem;
using _Scripts.Maps.New_Map.MapGenerateData;
using UnityEngine;

namespace _Scripts.Maps.New_Map
{
    // 맵 생성이 끝났음을 알리는 이벤트. 생성기는 플레이어·적 시스템을 직접 참조하지 않고 이 이벤트만 낸다.
    // 플레이어 쪽은 PlayerSpawnPosition으로 플레이어를 옮기고, 적·보상 쪽은 Rooms의 역할과 생성 지점을 쓴다.
    public class MapGeneratedEvent : IEvent
    {
        public int Seed { get; private set; }
        public IReadOnlyList<GeneratedRoom> Rooms { get; private set; }
        public Vector3 PlayerSpawnPosition { get; private set; }
        public bool IsFallback { get; private set; }   // 기본 맵을 썼는지

        public MapGeneratedEvent Init(int seed, IReadOnlyList<GeneratedRoom> rooms, Vector3 playerSpawnPosition, bool isFallback)
        {
            Seed = seed;
            Rooms = rooms;
            PlayerSpawnPosition = playerSpawnPosition;
            IsFallback = isFallback;
            return this;
        }
    }

    public static class MapGenerateEvents
    {
        private static readonly MapGeneratedEvent MapGenerated = new();

        public static void RaiseMapGenerated(int seed, IReadOnlyList<GeneratedRoom> rooms, Vector3 playerSpawnPosition, bool isFallback)
            => EventBus<MapGeneratedEvent>.Raise(MapGenerated.Init(seed, rooms, playerSpawnPosition, isFallback));
    }
}
