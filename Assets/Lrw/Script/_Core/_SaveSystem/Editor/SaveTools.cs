using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lrw.Script._Core._SaveSystem.Editor
{
    public static class SaveTools
    {
        [MenuItem("Tools/Save Folder")]
        private static void ShowWindow()
        {
            Application.OpenURL("file://" + SaveSystem.SaveDirectory);
        }
        
        
        
    }
}