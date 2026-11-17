using _Scripts._Core.SceneSystem;
using _Scripts.Agent.Players.InteractSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Maps.MapMoveSystem
{
    public class MoveSceneObject : AbstractInteractObject
    {
        [SerializeField] private SceneSo targetScene;
        
        public override void Interact(ModuleOwner owner)
        {
            SceneManager.LoadScene(targetScene);
        }
        
        
        
    }
}