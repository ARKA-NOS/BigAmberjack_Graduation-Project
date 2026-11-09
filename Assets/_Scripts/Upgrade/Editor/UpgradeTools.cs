using _Scripts._Core._SaveSystem;
using _Scripts._Core._SaveSystem.Data;
using UnityEditor;

namespace _Scripts.Upgrade.Editor
{
    public static class UpgradeTools
    {
        [MenuItem("Tools/Delete UpgradeData")]
        private static void DeleteUpgradeData()
        {
            SaveSystem.Delete<JsonDict<string, JsonDict<string, bool>>>(UpgradeManager.UpgradeDataSaveName);
        }
        
    }
}