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
        
        private void CreateGUI()
        {
            
            
        }
        
        
    }
}