using _Scripts._Core._Debug;
using UnityEngine;

namespace _Scripts._Core
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
            switch (dir)
            {
                case DirEnum.Left:
                    return Vector2.left;
                case DirEnum.Right:
                    return Vector2.right;
                case DirEnum.Up:
                    return Vector2.up;
                case DirEnum.Down:
                    return Vector2.down;
                default:
                    FDebug.LogError("[DirEnumExtensions.GetVector2] Unknown dir: " + dir);
                    return Vector2.zero;
            }
        }
    }
    
}