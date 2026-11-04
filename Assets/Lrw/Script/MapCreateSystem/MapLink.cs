using System;
using Lrw.Script._Core;
using UnityEngine;

namespace Lrw.Script.MapCreateSystem
{
    public class MapLink : MonoBehaviour
    {
        [SerializeField] private DirEnum direction;
        [SerializeField] private int linkSize;
        
        
        private void OnDrawGizmos()
        {
            for (int i = linkSize; i <= linkSize; i++)
            {
                /*Vector2 drawPos = transform.position + (direction.GetVector2())
                DrawBox()*/
            }
        }

        private void DrawBox(Vector2 pos)
        {
            Gizmos.DrawCube(pos, Vector3.one);
        }
        
    }
}