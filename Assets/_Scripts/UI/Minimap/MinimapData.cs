using System.Collections.Generic;
using CTJ.Enemies;
using SDW.Scripts.Maps;
using SDW.Scripts.Maps.Spawn;
using UnityEngine;

namespace SDW.Scripts.Minimap
{
    // 미니맵이 현재 방을 그리는 데 필요한 값. 방에 들어갈 때 MinimapPresenter가 한 번 Bind한다.
    // 적은 웨이브로 나중에 생기고 상자/NPC는 입장 직후에 생기므로, 아이콘 목록은 Bind 시점에 복사하지 않고
    // Binder가 매 프레임 CollectMarkers로 방에서 직접 읽어 간다.
    public class MinimapData
    {
        private readonly RoomController _room;
        private readonly InteractableSpawnPoint[] _interactablePoints;

        // 방 지형(충돌 타일)을 칠한 텍스처. WorldRect 전체에 늘려서 그린다.
        public Texture2D Terrain { get; }
        // 방 타일맵 전체를 감싸는 월드 범위.
        public Rect WorldRect { get; }
        public Transform Player { get; }

        public MinimapData(RoomController room, Texture2D terrain, Rect worldRect, Transform player)
        {
            _room = room;
            _interactablePoints = room.GetComponentsInChildren<InteractableSpawnPoint>(true);
            Terrain = terrain;
            WorldRect = worldRect;
            Player = player;
        }

        public void CollectMarkers(List<MinimapMarker> result)
        {
            result.Clear();

            if (_room == null)
                return;

            foreach (EnemyBase enemy in _room.SpawnedEnemies)
            {
                if (RoomController.IsAlive(enemy))
                    result.Add(new MinimapMarker(enemy.transform.position, MinimapMarkerType.Enemy));
            }

            foreach (InteractableSpawnPoint point in _interactablePoints)
            {
                if (point == null || point.Spawned == null)
                    continue;

                if (TryGetMarkerType(point.Spawned, out MinimapMarkerType type))
                    result.Add(new MinimapMarker(point.Spawned.transform.position, type));
            }
        }

        private static bool TryGetMarkerType(Component interactable, out MinimapMarkerType type)
        {
            switch (interactable)
            {
                case Interact.Chest.Chest:
                    type = MinimapMarkerType.Chest;
                    return true;
                case Interact.NPC.Npc:
                    type = MinimapMarkerType.Npc;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }
    }
}
