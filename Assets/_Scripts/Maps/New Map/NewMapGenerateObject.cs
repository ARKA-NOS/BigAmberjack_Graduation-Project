using _Scripts._Core._EventSystem;
using _Scripts.Agent.Players.InteractSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Maps.New_Map
{
    public class NewMapGenerateObject : MonoBehaviour, ICanInteract
    {
        public Vector2 Position => transform.position;
        
        public void Interact(ModuleOwner owner)
            => EventBus<MapMoveEvent>.Raise(new MapMoveEvent());
        
    }
}