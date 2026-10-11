using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lrw.Script._Core._SaveSystem.Editor
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