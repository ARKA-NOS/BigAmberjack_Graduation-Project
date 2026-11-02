using UnityEngine;

namespace Lrw.Script._Core.SceneSystem
{
    [CreateAssetMenu(fileName = "Scene SO", menuName = "SceneSystem/Scene SO", order = 0)]
    public class SceneSo : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField] private UnityEditor.SceneAsset scene;
        
        private void OnEnable()
        {
            OnValidate();
        }
        
        private void OnValidate()
        {
            SceneName = scene?.name ?? "";
        }
#endif
        
        [field:SerializeField] public string SceneName { get; private set; }
        
        public static implicit operator string(SceneSo so) => so.SceneName;
    }
}