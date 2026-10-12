using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lrw.Script._Core.SceneSystem
{
    [CreateAssetMenu(fileName = "Scene SO", menuName = "SceneSystem/Scene SO", order = 0)]
    public class SceneSo : ScriptableObject
    {
        [SerializeField] private SceneAsset scene;
        [field:SerializeField] public string SceneName { get; private set; }
        
        private void OnEnable()
        {
            OnValidate();
        }
        
        private void OnValidate()
        {
            SceneName = scene?.name ?? "";
        }
        
        public static implicit operator string(SceneSo so) => so.SceneName;
    }
}