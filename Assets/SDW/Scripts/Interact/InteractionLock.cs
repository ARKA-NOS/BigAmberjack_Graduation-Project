using System;
using System.Collections;
using SDW.Scripts.Maps;
using UnityEngine;

namespace SDW.Scripts.Interact
{
    // 상자/NPC 같은 상호작용 오브젝트에 붙여서, 주변 반경 안에 살아 있는 적이 있으면 상호작용을 잠근다.
    // 적을 모두 처치하거나 적이 반경 밖으로 나가면 다시 열린다. (출구 Portal에는 붙이지 않는다)
    // 반경 안에 웨이브형 전투 구역이 진행 중이면, 적 유무와 상관없이 마지막 웨이브가 끝날 때까지 잠근다.
    // 실제로 상호작용을 막는 건 이 컴포넌트를 구독하는 쪽(Chest, Npc)이 LockChanged를 받아 처리한다.
    public class InteractionLock : MonoBehaviour
    {
        private const float CheckInterval = 0.2f;

        [Tooltip("이 반경 안에 살아 있는 적이 있으면 잠긴다.")]
        [SerializeField, Min(0f)] private float lockRadius = 8f;
        [Tooltip("잠겨 있을 때 켜지는 표시(자물쇠 아이콘 등). 비워두면 표시하지 않는다.")]
        [SerializeField] private GameObject lockedVisual;

        private RoomController _room;
        private Coroutine _routine;

        public bool IsLocked { get; private set; }

        public event Action<bool> LockChanged;

        private void Awake()
        {
            _room = GetComponentInParent<RoomController>();

            if (lockedVisual != null)
                lockedVisual.SetActive(false);
        }

        private void OnEnable()
        {
            _routine = StartCoroutine(CheckRoutine());
        }

        private void OnDisable()
        {
            if (_routine != null)
                StopCoroutine(_routine);

            _routine = null;
        }

        private IEnumerator CheckRoutine()
        {
            WaitForSeconds interval = new WaitForSeconds(CheckInterval);

            while (true)
            {
                Refresh();
                yield return interval;
            }
        }

        private void Refresh()
        {
            // 방 밖에 놓인 오브젝트는 잠그지 않는다.
            bool locked = _room != null && _room.IsInteractionLocked(transform.position, lockRadius);

            if (locked == IsLocked)
                return;

            IsLocked = locked;

            if (lockedVisual != null)
                lockedVisual.SetActive(locked);

            LockChanged?.Invoke(locked);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, lockRadius);
        }
    }
}
