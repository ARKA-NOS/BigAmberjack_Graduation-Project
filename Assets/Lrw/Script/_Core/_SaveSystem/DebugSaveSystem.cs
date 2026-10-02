using System;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace Lrw.Script._Core._SaveSystem
{
    public class DebugSaveSystem : MonoBehaviour
    {
        [SerializeField] private string saveName;
        [SerializeField] private TestSaveData data;

        [ContextMenu("Save")]
        private void Save()
        {
            SaveSystem.Save(saveName,data);
        }

        [ContextMenu("Load")]
        private void Load()
        {
            data = SaveSystem.Load(saveName,new TestSaveData() { number = 1, name = "Default" });
        }
        
        [ContextMenu("Open Directory")]
        public void OpenDirectory()
        {
            EditorUtility.RevealInFinder(SaveSystem.SaveDirectory);
        }
        
    }
}