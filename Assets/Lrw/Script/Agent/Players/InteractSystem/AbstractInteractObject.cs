using System;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.InteractSystem
{
    public abstract class AbstractInteractObject : MonoBehaviour, ICanInteract
    {
        [SerializeField] private Vector2 interactUIOffset;
        public Vector2 Position => transform.position + (Vector3)interactUIOffset;
        public abstract void Interact(ModuleOwner owner);

        protected void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(Position,0.5f);   
        }
        
        
    }
}