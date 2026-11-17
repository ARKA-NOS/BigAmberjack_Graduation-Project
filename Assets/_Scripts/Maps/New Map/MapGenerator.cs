using System.Collections.Generic;
using System.Threading;
using _Scripts._Core._Debug;
using _Scripts.DataStructure;
using _Scripts.Loading;
using _Scripts.Maps.New_Map.MapGenerateData;
using _Scripts.Maps.New_Map.MapSettingAsset;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Scripts.Maps.New_Map
{
    /*
     * 생성 파이프라인
     * 언로드 → 로드 → 배치(MapDeployment, 스레드풀) → 프리팹 생성 → 연결 지점 마감 → 벽 채우기 → 완료 이벤트 → End
     * 실패하면 프리팹 생성 대신 기본 맵(FallbackMapPrefab)을 만들고, 그 경우에도 벽 채우기와 완료 이벤트는 한 번씩 한다.
     */
    public partial class MapGenerator : MonoBehaviour
    {
        private const string WallFillLoadingText = "Wall Fill";

        [SerializeField] private MapGenerateSetting mapGenerateSetting;
        // 비어 있으면 자식에서 찾는다.
        [SerializeField] private MapWallFiller wallFiller;

        private MapGenerateSetting prevMapSetting;

        // 이번 생성에서 만든 방(또는 기본 맵) 인스턴스. 다시 생성할 때 먼저 지운다.
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
        private CancellationTokenSource _generateCts;
        private int _currentSeed;

        private void Awake()
        {
            if (wallFiller == null)
                wallFiller = GetComponentInChildren<MapWallFiller>(true);

            GenerateMap(mapGenerateSetting);
        }

        private void OnDestroy()
        {
            _generateCts?.Cancel();
            _generateCts?.Dispose();
            _generateCts = null;
        }

        #region MapGenerate

        public void GenerateMap(MapGenerateSetting mapSetting)
        {
            if (mapSetting == null)
            {
                FDebug.LogError("[MapGenerator] MapGenerateSetting is null");
                return;
            }

            GenerateMap(mapSetting, mapSetting.Seed);
        }

        public void GenerateMap(MapGenerateSetting mapSetting, int seed)
        {
            if (mapSetting == null)
            {
                FDebug.LogError("[MapGenerator] MapGenerateSetting is null");
                return;
            }

            // 진행 중인 생성이 있으면 취소하고 새로 시작한다.
            _generateCts?.Cancel();
            _generateCts?.Dispose();
            _generateCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            _currentSeed = seed;
            GenerateMapUniTask(mapSetting, seed, _generateCts.Token).Forget();
        }

        private async UniTaskVoid GenerateMapUniTask(MapGenerateSetting mapSetting, int seed, CancellationToken ct)
        {
            // 오류가 난 맵을 같은 시드로 다시 확인할 수 있도록 시드를 먼저 남긴다.
            FDebug.Log($"[MapGenerator] Seed={seed}");

            ClearGeneratedMap();

            await UnLoadAssets(ct);

            await LoadAssets(mapSetting, ct);

            DeploymentResult deployment = await MapDeployment(mapSetting, seed, ct);

            List<GeneratedRoom> rooms;
            if (deployment.IsFallback)
            {
                rooms = InstantiateFallbackMap(mapSetting, seed, deployment);
            }
            else
            {
                Room[] instances = await InstantiatePrefabs(deployment.Rooms, ct);
                SealLinks(instances, deployment.Rooms);
                rooms = CreateGeneratedRooms(instances, deployment.Rooms);

                FDebug.Log($"[MapGenerator] 생성 완료 Seed={seed}, 방 {rooms.Count}개, 되돌리기 {deployment.Backtracks}회, 전체 재생성 {deployment.FullRetries}회");
            }

            await FillWalls(mapSetting, rooms, ct);

            RaiseMapGenerated(seed, rooms, deployment.IsFallback);
            SetLoadingGauge("InstantiateRoom", 100, 100);

            End(mapSetting);
        }

        #region Addressable

        private async UniTask UnLoadAssets(CancellationToken ct)
        {
            if(prevMapSetting == null) return;

            await UniTask.Yield(ct);
            // 나중에
        }

        private async UniTask LoadAssets(MapGenerateSetting mapSetting, CancellationToken ct)
        {
            await UniTask.Yield(ct);
            // 나중에
        }

        #endregion

        #region Instantiate

        // 결과 배열은 roomDataList와 같은 순서다. 생성에 실패한 방은 null이다.
        private async UniTask<Room[]> InstantiatePrefabs(List<RoomDeploymentData> roomDataList, CancellationToken ct)
        {
            await UniTask.SwitchToMainThread(ct);

            List<UniTask<Room>> taskList = new List<UniTask<Room>>();

            SetLoadingGauge("InstantiateRoom", 0, 100);

            foreach (RoomDeploymentData roomDeploymentData in roomDataList)
            {
                var task = InstantiateRoomAsync(roomDeploymentData, ct);
                taskList.Add(task);
            }

            
            Room[] rooms = await UniTask.WhenAll(taskList);

            await UniTask.SwitchToMainThread(ct);

            SetLoadingGauge("InstantiateRoom", 100, 100);
            return rooms;
        }

        private async UniTask<Room> InstantiateRoomAsync(RoomDeploymentData roomData, CancellationToken ct)
        {
            var op = InstantiateAsync(roomData.RoomPrefab, transform, roomData.Position, Quaternion.identity);

            using (ct.Register(op.Cancel))
            {
                await op.ToUniTask(cancellationToken: ct);
            }

            await UniTask.SwitchToMainThread(ct);

            Room room = op.Result != null && op.Result.Length > 0 ? op.Result[0] : null;
            if (room != null)
            {
                room.name = $"{roomData.NodeId} ({roomData.RoomPrefab.name})";
                _spawnedObjects.Add(room.gameObject);
            }

            return room;
        }

        // 6단계: 쓰지 않은 연결 지점은 막고, 연결된 지점은 연다.
        private static void SealLinks(Room[] instances, List<RoomDeploymentData> roomDataList)
        {
            for (int i = 0; i < instances.Length; i++)
            {
                if (instances[i] != null)
                    instances[i].ApplyLinkState(roomDataList[i].ConnectedLinks);
            }
        }

        private static List<GeneratedRoom> CreateGeneratedRooms(Room[] instances, List<RoomDeploymentData> roomDataList)
        {
            List<GeneratedRoom> rooms = new List<GeneratedRoom>(instances.Length);
            for (int i = 0; i < instances.Length; i++)
            {
                if (instances[i] != null)
                    rooms.Add(new GeneratedRoom(instances[i], roomDataList[i].Role, roomDataList[i].NodeId));
            }
            return rooms;
        }

        // 생성에 실패했을 때 미리 만든 기본 맵을 쓴다.
        private List<GeneratedRoom> InstantiateFallbackMap(MapGenerateSetting mapSetting, int seed, DeploymentResult deployment)
        {
            List<GeneratedRoom> rooms = new List<GeneratedRoom>();

            GameObject prefab = mapSetting.FallbackMapPrefab;
            if (prefab == null)
            {
                FDebug.LogError($"[MapGenerator] 맵 생성에 실패했고 FallbackMapPrefab도 없습니다. Seed={seed}", mapSetting);
                return rooms;
            }

            FDebug.LogWarning($"[MapGenerator] 맵 생성에 실패해 기본 맵을 사용합니다. Seed={seed}, 되돌리기 {deployment.Backtracks}회, 전체 재생성 {deployment.FullRetries}회", mapSetting);

            GameObject instance = Instantiate(prefab, transform);
            _spawnedObjects.Add(instance);

            // 기본 맵 안 방의 역할은 알 수 없으므로 첫 번째 방을 시작방, 나머지를 전투방으로 둔다.
            Room[] found = instance.GetComponentsInChildren<Room>(true);
            for (int i = 0; i < found.Length; i++)
                rooms.Add(new GeneratedRoom(found[i], i == 0 ? RoomRole.Start : RoomRole.Combat, $"Fallback{i}"));

            return rooms;
        }

        private void ClearGeneratedMap()
        {
            foreach (GameObject spawned in _spawnedObjects)
            {
                if (spawned != null)
                    Destroy(spawned);
            }
            _spawnedObjects.Clear();

            if (wallFiller != null)
                wallFiller.Clear();
        }

        #endregion

        #region Wall

        // 연결 지점 마감 다음 단계: 방 사이와 맵 바깥 가장자리의 빈 칸을 벽 타일로 채운다.
        // 완료 이벤트보다 먼저 해서, 이벤트를 받는 쪽은 벽까지 다 만들어진 맵을 본다.
        private async UniTask FillWalls(MapGenerateSetting mapSetting, List<GeneratedRoom> rooms, CancellationToken ct)
        {
            await UniTask.SwitchToMainThread(ct);

            SetLoadingGauge(WallFillLoadingText, 0, 1);

            if (wallFiller == null)
            {
                FDebug.LogWarning("[MapGenerator] MapWallFiller가 없어 벽을 채우지 않습니다.", this);
            }
            else if (mapSetting.WallTile == null)
            {
                FDebug.LogWarning("[MapGenerator] WallTile이 없어 벽을 채우지 않습니다.", mapSetting);
            }
            else
            {
                await wallFiller.Fill(rooms, mapSetting.WallTile, mapSetting.WallPadding, mapSetting.WallTilesPerFrame,
                    (current, max) => SetLoadingGauge(WallFillLoadingText, current, max), ct);
            }

            SetLoadingGauge(WallFillLoadingText, 1, 1);
        }

        #endregion

        #region Event

        // 7·8단계: 적·보상 배치와 플레이어 이동은 이 이벤트를 구독한 쪽이 한다.
        private static void RaiseMapGenerated(int seed, List<GeneratedRoom> rooms, bool isFallback)
            => MapGenerateEvents.RaiseMapGenerated(seed, rooms, GetPlayerSpawnPosition(rooms), isFallback);

        // 시작방의 playerSpawnPoint 위치. 시작방을 알 수 없으면 첫 번째 방을 쓴다.
        private static Vector3 GetPlayerSpawnPosition(List<GeneratedRoom> rooms)
        {
            if (rooms.Count == 0) return Vector3.zero;

            int startIndex = rooms.FindIndex(r => r.Role == RoomRole.Start);
            Room room = rooms[startIndex >= 0 ? startIndex : 0].Instance;

            Transform point = room.GetPlayerSpawnPoint();
            if (point != null) return point.position;

            FDebug.LogWarning($"[MapGenerator] {room.name} : playerSpawnPoint가 없어 방 위치를 플레이어 생성 위치로 씁니다.", room);
            return room.transform.position;
        }

        #endregion

        private void End(MapGenerateSetting mapSetting)
        {
            prevMapSetting = mapSetting;
        }

        #endregion

        #region Debug

        // 지금 보이는 맵을 같은 시드로 다시 만든다.
        [ContextMenu("맵 재생성 (현재 시드)")]
        private void RegenerateWithCurrentSeed()
        {
            if (!CanRegenerate()) return;
            GenerateMap(mapGenerateSetting, _currentSeed);
        }

        [ContextMenu("맵 재생성 (랜덤 시드)")]
        private void RegenerateWithRandomSeed()
        {
            if (!CanRegenerate()) return;
            GenerateMap(mapGenerateSetting, new System.Random().Next());
        }

        private bool CanRegenerate()
        {
            if (Application.isPlaying) return true;

            FDebug.LogWarning("[MapGenerator] 맵 재생성은 플레이 모드에서만 할 수 있습니다.", this);
            return false;
        }

        #endregion

        private static void SetLoadingGauge(string text, int current, int max)
            => LoadingEvents.RaiseSetLoadingGauge(text, new Gauge(current,max));

    }
}
