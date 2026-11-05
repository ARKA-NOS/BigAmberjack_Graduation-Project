using Lrw.Script._Core;
using UnityEngine;

namespace Lrw.Script.MapCreateSystem
{
    public class RoomLink : MonoBehaviour
    {
        [SerializeField] private DirEnum direction;
        [SerializeField] private int linkSize;
        
        private void OnDrawGizmos()
        {
            for (int i = -linkSize; i <= linkSize; i++)
            {
                Vector2 drawPos = transform.position + (Quaternion.Euler(0,0,90f) * direction.GetVector2() * i);
                DrawBox(drawPos);
            }
            Gizmos.color = Color.yellow;
            DrawLineGizmo(transform.position, transform.position + (Vector3)direction.GetVector2());
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
            => direction.GetVector2() * linkSize;
        
        public static bool CanLink(RoomLink link1, RoomLink link2)
            => link1.GetCheckVector() - link2.GetCheckVector() == Vector2.zero;
        
    }
}