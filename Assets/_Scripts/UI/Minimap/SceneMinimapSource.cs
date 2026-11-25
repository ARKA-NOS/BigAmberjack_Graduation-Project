using System;
using System.Collections.Generic;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players;
using _Scripts.Maps.New_Map;
using _Scripts.Maps.New_Map.BossMapSystem;
using _Scripts.Maps.New_Map.MapSettingAsset;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace _Scripts.UI.Minimap
{
    // 지금 씬의 지형과 서비스 로케이터의 플레이어를 IMinimapSource로 감싼다. MinimapConnector가 만든다.
    // - 맵 생성기가 있는 씬: 일반 맵은 생성된 방들(안개), 보스 맵은 BossMap(전체 공개)
    // - 맵 생성기가 없는 씬(로비): 씬의 충돌 타일맵 전체(전체 공개)
    public class SceneMinimapSource : IMinimapSource
    {
        // 연결 지점을 막는 타일맵의 이름 접두사. 충돌체가 없어도 벽으로 보이므로 함께 그린다.
        private const string SealNamePrefix = "Seal";

        // 읽을 때마다 씬을 검색하므로 연결 시점과 AreaChanged에서만 읽는다.
        public MinimapArea CurrentArea
        {
            get
            {
                if (Object.FindAnyObjectByType<MapGenerator>() == null)
                    return CreateArea(CollectSceneTilemaps(), false);

                if (!MinimapMapRecorder.IsBossMap)
                    return CreateArea(CollectRoomTilemaps(), true);

                BossMap bossMap = Object.FindAnyObjectByType<BossMap>();
                return bossMap != null ? CreateArea(CollectColliderTilemaps(bossMap.transform), false) : null;
            }
        }

        // 매 프레임 불리므로 씬 검색 대신 서비스 로케이터에서 가져온다. 플레이어가 없으면 Null 객체가 등록되어 있어 null이 된다.
        public Transform Player =>
            ServiceLocator.Get<IPlayerController>() is Component player && player != null ? player.transform : null;

        public event Action AreaChanged
        {
            add => MinimapMapRecorder.Changed += value;
            remove => MinimapMapRecorder.Changed -= value;
        }

        private static MinimapArea CreateArea(List<Tilemap> tilemaps, bool usesFog)
            => tilemaps.Count > 0 ? new MinimapArea(tilemaps, usesFog) : null;

        // 방 바깥을 채우는 벽 타일맵은 방의 자식이 아니라서 들어가지 않는다. 열린 통로의 봉인벽은 비활성이라 빠진다.
        private static List<Tilemap> CollectRoomTilemaps()
        {
            List<Tilemap> result = new();

            foreach (Room room in MinimapMapRecorder.GeneratedRooms)
            {
                if (room == null) continue;

                foreach (Tilemap tilemap in room.GetComponentsInChildren<Tilemap>(false))
                {
                    if (tilemap.TryGetComponent(out TilemapCollider2D _) || tilemap.name.StartsWith(SealNamePrefix))
                        result.Add(tilemap);
                }
            }

            return result;
        }

        private static List<Tilemap> CollectColliderTilemaps(Transform root)
        {
            List<Tilemap> result = new();

            foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(false))
            {
                if (tilemap.TryGetComponent(out TilemapCollider2D _))
                    result.Add(tilemap);
            }

            return result;
        }

        private static List<Tilemap> CollectSceneTilemaps()
        {
            List<Tilemap> result = new();

            foreach (TilemapCollider2D collider in Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None))
            {
                if (collider.TryGetComponent(out Tilemap tilemap))
                    result.Add(tilemap);
            }

            return result;
        }
    }
}
