using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lrw.Script._Core._SaveSystem.Editor
{
    public class SaveDataWindow : EditorWindow
    {
        [MenuItem("Tools/Save Data Window")]
        private static void ShowWindow()
        {
            var window = GetWindow<SaveDataWindow>();
            window.titleContent = new GUIContent("Save Data Window");
            window.Show();
        }
        
        private string[] _saveFiles;
        private string[] _fileData;
        private void CreateGUI()
        {
            string[] fileNames = SaveSystem.GetFilePaths();
            
            _fileData = fileNames.Select(SaveSystem.LoadText).ToArray();

            foreach (string data in _fileData)
            {
                FDebug.Log(data);
            }
            
        }
        
        
    }
}