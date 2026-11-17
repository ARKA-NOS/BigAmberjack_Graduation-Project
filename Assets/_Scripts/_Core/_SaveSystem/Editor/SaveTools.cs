using UnityEditor;
using UnityEngine;

namespace _Scripts._Core._SaveSystem.Editor
{
    public static class SaveTools
    {
        [MenuItem("Tools/Open Save Folder")]
        private static void OpenSaveFolder()
        {
            Application.OpenURL("file://" + SaveSystem.SaveDirectory);
        }
        
        
        
    }
}