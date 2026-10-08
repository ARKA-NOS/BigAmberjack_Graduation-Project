using UnityEngine;

namespace Lrw.Script.Agent.ControlSystem
{
    public struct PlayContext
    {
        public Vector2 Position;
        public float Power;
        public bool IsCritical;

        public PlayContext(Vector2 position, float power, bool isCritical = false)
        {
            Position =  position;
            Power = power;
            IsCritical = isCritical;
        }
        
    }
}