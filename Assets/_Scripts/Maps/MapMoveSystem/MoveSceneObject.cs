using _Scripts._Core._Debug;
using _Scripts._Core.SceneSystem;
using _Scripts.Agent.Players.InteractSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Maps.MapMoveSystem
{
    // 상호작용하면 로딩창을 띄우고 targetScene으로 이동한다.
    // 이동한 씬에 맵 생성기가 있으면 맵 생성이 끝날 때까지 로딩창이 남는다.
    public class MoveSceneObject : AbstractInteractObject
    {
        [SerializeField] private SceneSo targetScene;

        public override void Interact(ModuleOwner owner)
        {
            if (targetScene == null)
            {
                FDebug.LogWarning($"[{name}] targetScene이 지정되지 않았습니다.", this);
                return;
            }

            SceneLoader.LoadScene(targetScene);
        }
    }
}
