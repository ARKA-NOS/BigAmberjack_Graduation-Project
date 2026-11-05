using Lrw.Script._Core._Debug;
using UnityEngine;

namespace Lrw.Script._Core
{
    public enum DirEnum
    {
        Left,
        Right,
        Up,
        Down,
    }
    
    public static class DirEnumExtensions
    {
        public static Vector2 GetVector2(this DirEnum dir)
        {
            if(dir == DirEnum.Left)
                return Vector2.left;
            if(dir == DirEnum.Right)
                return Vector2.right;
            if (dir == DirEnum.Up)
                return Vector2.up;
            if (dir == DirEnum.Down)
                return Vector2.down;

            FDebug.LogError("[DirEnumExtensions.GetVector2] Unknown dir: " + dir);
            
            return Vector2.zero;
        }
    }
    
}