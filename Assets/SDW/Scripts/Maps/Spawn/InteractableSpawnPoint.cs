using Lrw.Script.Agent.Players.InteractSystem;
using UnityEngine;

namespace SDW.Scripts.Maps.Spawn
{
    // 방 프리팹에 배치하는 상호작용 오브젝트(상자, NPC, 상점 등) 스폰 위치.
    // 방에 처음 입장할 때 RoomController가 Spawn을 호출하고, 테이블이 정한 오브젝트를 이 위치에 생성한다.
    public class InteractableSpawnPoint : MonoBehaviour
    {
        [SerializeField] private InteractableSpawnTableSO table;

        [Header("Gizmos")]
        [SerializeField] private Color gizmoColor = new(0.27f, 0.84f, 0.36f);
        [SerializeField] private Vector2 gizmoSize = new(1f, 1f);

        public AbstractInteractObject Spawned { get; private set; }

        public void Spawn(RoomController room)
        {
            if (Spawned != null)
                return;

            if (table == null)
            {
                Debug.LogWarning($"{room.name}/{name} : Spawn Table이 지정되지 않았습니다.");
                return;
            }

            if (!table.TryPick(out AbstractInteractObject prefab))
                return;

            Spawned = Instantiate(prefab, transform.position, Quaternion.identity, room.transform);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, gizmoSize);
        }
    }
}
