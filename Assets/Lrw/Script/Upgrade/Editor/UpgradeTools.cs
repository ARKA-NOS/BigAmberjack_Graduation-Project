using Lrw.Script._Core._SaveSystem;
using Lrw.Script._Core._SaveSystem.Data;
using UnityEditor;

namespace Lrw.Script.Upgrade.Editor
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