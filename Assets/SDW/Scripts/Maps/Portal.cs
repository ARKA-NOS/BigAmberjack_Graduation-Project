using Agents.Players;
using SDW.Scripts.CoreSystem;
using SDW.Scripts.UI;
using UnityEngine;

namespace SDW.Scripts.Maps
{
    // 방과 방 사이를 연결하는 포탈.
    // 플레이어가 트리거에 닿으면 화면을 Fade로 가린 상태에서 현재 방을 비활성화하고
    // 목표 방을 활성화한 뒤, 목표 방의 대응 포탈 위치로 플레이어를 이동시킨다.
    public class Portal : InteractableBase
    {
        [SerializeField] private float arrivalOffset = 1.5f;

        public RoomDirection Direction { get; private set; }
        public RoomNode TargetRoom { get; private set; }

        public override bool CanInteract => !_isTransitioning;

        private DungeonGenerator _dungeonGenerator;
        private ScreenFader _screenFader;
        private Collider2D _col;
        private PlayerController _pendingPlayer;

        private static bool _isTransitioning;

        public void Initialize(RoomDirection direction, RoomNode targetRoom, DungeonGenerator dungeonGenerator, ScreenFader screenFader)
        {
            Direction = direction;
            TargetRoom = targetRoom;
            _dungeonGenerator = dungeonGenerator;
            _screenFader = screenFader;
        }

        private void Awake()
        {
            // Portal Prefab에 Collider2D가 미리 세팅되어 있지 않은 경우를 대비한 안전장치.
            _col = GetComponent<Collider2D>();
            Debug.Assert(_col != null, "포탈 콜라이더가 없습니다.");
        }

        // TODO: 지금은 PlayerController 컴포넌트를 직접 찾아 감지하지만,
        // 추후 이벤트 버스 기반 감지로 교체 예정.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!CanInteract)
                return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
                return;

            _pendingPlayer = player;
            Interaction();
        }

        public override void Interaction()
        {
            if (_dungeonGenerator == null)
            {
                Debug.LogWarning($"{name} : DungeonGenerator 참조가 없어 이동할 수 없습니다.");
                return;
            }

            GameObject targetInstance = _dungeonGenerator.GetRoomInstance(TargetRoom);
            if (targetInstance == null)
            {
                Debug.LogWarning($"{name} : 이동할 목표 방 인스턴스를 찾을 수 없습니다.");
                return;
            }

            GameObject currentRoomInstance = transform.parent != null ? transform.parent.gameObject : null;
            Vector3 spawnPosition = GetArrivalPosition(targetInstance);
            PlayerController player = _pendingPlayer;

            _isTransitioning = true;

            _screenFader.Transition(() =>
            {
                if (currentRoomInstance != null)
                    currentRoomInstance.SetActive(false);

                targetInstance.SetActive(true);

                player.transform.position = spawnPosition;

                Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
                if (playerBody != null)
                    playerBody.position = spawnPosition;

                _isTransitioning = false;
            });
        }

        // 목표 방에서 이번 포탈과 대응하는(반대 방향) 포탈 위치를 찾아,
        // 이동해온 방향으로 조금 더 들어간 지점을 도착 위치로 삼는다.
        private Vector3 GetArrivalPosition(GameObject targetInstance)
        {
            RoomDefinition targetDefinition = targetInstance.GetComponent<RoomDefinition>();
            Transform arrivalPoint = targetDefinition != null ? targetDefinition.GetPortalPoint(Direction.Opposite()) : null;

            Vector3 basePosition = arrivalPoint != null ? arrivalPoint.position : targetInstance.transform.position;
            Vector3 inwardDirection = (Vector3)(Vector2)Direction.ToVector2Int();

            return basePosition + inwardDirection * arrivalOffset;
        }
    }
}
