using DevLib.ModuleSystem;
using Lrw.Script._Core.SceneSystem;
using Lrw.Script.Player.InteractSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lrw.Script.Map
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