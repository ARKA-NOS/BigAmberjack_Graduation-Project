using System;
using UnityEngine;

namespace LrwLib.UnityPosition
{
    [Serializable]
    public class UnityPos
    {
        [SerializeField] private Vector3 position;

        public Vector3 Position
        {
            get => position;
            set => position = value;
        }

        public UnityPos()
        {
        }

        public UnityPos(Vector3 position)
        {
            this.position = position;
        }

        public static implicit operator Vector3(UnityPos unityPos)
        {
            return unityPos?.position ?? Vector3.zero;
        }
        
    }
}
