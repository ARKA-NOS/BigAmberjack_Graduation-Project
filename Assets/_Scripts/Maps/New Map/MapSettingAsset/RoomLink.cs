using _Scripts._Core;
using UnityEngine;

namespace _Scripts.Maps.New_Map.MapSettingAsset
{
    public class RoomLink : MonoBehaviour
    {
        [field:SerializeField] public DirEnum Direction { get; private set; }
        [field:SerializeField] public int LinkSize { get; private set; }

        // 연결되지 않았을 때 켜서 통로를 막는 벽(타일맵 자식 등)
        [SerializeField] private GameObject sealObject;

        public void SetConnected(bool connected)
        {
            if (sealObject != null)
                sealObject.SetActive(!connected);
        }

        private void OnDrawGizmos()
        {
            for (int i = -LinkSize; i <= LinkSize; i++)
            {
                Vector2 drawPos = transform.position + (Quaternion.Euler(0,0,90f) * Direction.GetVector2() * i);
                DrawBox(drawPos);
            }
            Gizmos.color = Color.yellow;
            DrawLineGizmo(transform.position, transform.position + (Vector3)Direction.GetVector2());
        }
        
        private void DrawBox(Vector2 pos)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(pos, Vector3.one);
        }
        
        private void DrawLineGizmo(Vector3 start, Vector3 end)
        {
            Vector3 dir = end - start;
            Vector3 normalDir = dir.normalized;
            Vector3 arrowStart = end - normalDir * 0.25f;
            Vector3 arrowEnd = end - normalDir * 0.15f;

            const float arrowSize = 0.05f;
            Vector3 triPointA = arrowStart + Quaternion.Euler(0, 0, -90) * normalDir * arrowSize;
            Vector3 triPointB = arrowStart + Quaternion.Euler(0, 0, 90) * normalDir * arrowSize;
            
            Gizmos.DrawLine(start, arrowStart);
            Gizmos.DrawLine(triPointA, arrowEnd);
            Gizmos.DrawLine(triPointB, arrowEnd);
            Gizmos.DrawLine(triPointA, triPointB);
        }
        
        private Vector2 GetCheckVector()
            => Direction.GetVector2() * LinkSize;
        
        
        
    }
}