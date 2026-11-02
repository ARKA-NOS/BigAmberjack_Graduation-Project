using DevLib.ModuleSystem;
using Lrw.Script.Agent.Players.InteractSystem;
using SDW.Scripts.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace SDW.Scripts.Maps
{
    // 다음 방으로만 가는 단방향 출구.
    // 플레이어의 InteractModule이 이 오브젝트를 찾아 상호작용 키를 누르면, 화면을 Fade로 가린 상태에서
    // 현재 방을 비활성화하고 다음 방을 활성화한 뒤 다음 방의 Player Spawn Point로 플레이어를 옮긴다.
    // InteractModule은 MapObject 레이어의 콜라이더에서 이 컴포넌트를 찾으므로, 콜라이더와 같은 오브젝트에 두어야 한다.
    public class Portal : AbstractInteractObject
    {
        [Tooltip("출구가 잠겨 있을 때 켜지는 표시(닫힌 문 등). 비워두면 표시 없이 상호작용만 막는다.")]
        [SerializeField, FormerlySerializedAs("doorVisual")] private GameObject lockedVisual;

        public RoomController OwnerRoom { get; private set; }
        public RoomController TargetRoom { get; private set; }
        public bool IsLocked { get; private set; }

        private ScreenFader _screenFader;
        private Collider2D _col;

        private static bool _isTransitioning;

        // 전환(Fade) 도중 씬이 바뀌면 코루틴이 함께 사라져 _isTransitioning이 true로 남는다.
        // 탐험이 끝나 씬을 떠날 때 RunResetter가 호출해서 다음 탐험의 포탈이 막히지 않게 한다.
        public static void ResetTransitionState() => _isTransitioning = false;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            Debug.Assert(_col != null, "포탈 콜라이더가 없습니다.");
        }

        public void Initialize(RoomController ownerRoom, RoomController targetRoom, ScreenFader screenFader)
        {
            OwnerRoom = ownerRoom;
            TargetRoom = targetRoom;
            _screenFader = screenFader;
        }

        // 방의 출구 조건(RoomDataSO.clearCondition)을 달성하기 전까지 잠근다. RoomController가 호출한다.
        // 잠기면 콜라이더를 꺼서 InteractModule의 상호작용 대상(과 프롬프트)에서 빠진다.
        public void SetLocked(bool locked)
        {
            IsLocked = locked;

            if (_col != null)
                _col.enabled = !locked;

            if (lockedVisual != null)
                lockedVisual.SetActive(locked);
        }

        public override void Interact(ModuleOwner owner)
        {
            if (IsLocked || _isTransitioning || owner == null)
                return;

            if (TargetRoom == null)
            {
                Debug.LogWarning($"{name} : 이동할 목표 방을 찾을 수 없습니다.");
                return;
            }

            RoomController ownerRoom = OwnerRoom;
            RoomController targetRoom = TargetRoom;
            Transform player = owner.transform;
            Vector3 spawnPosition = targetRoom.GetPlayerSpawnPosition();

            _isTransitioning = true;

            _screenFader.Transition(() =>
            {
                if (ownerRoom != null)
                    ownerRoom.gameObject.SetActive(false);

                targetRoom.gameObject.SetActive(true);
                targetRoom.Enter();

                player.position = spawnPosition;

                Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
                if (playerBody != null)
                {
                    playerBody.position = spawnPosition;
                    playerBody.linearVelocity = Vector2.zero;
                }

                _isTransitioning = false;
            });
        }
    }
}
