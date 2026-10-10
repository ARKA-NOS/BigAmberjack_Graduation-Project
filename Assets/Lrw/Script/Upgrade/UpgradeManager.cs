using System.Collections.Generic;
using System.Linq;
using Lrw.Script._Core._Debug;
using Lrw.Script._Core._Manager;
using Lrw.Script._Core._SaveSystem;
using Lrw.Script._Core._SaveSystem.Data;
using Lrw.Script.Upgrade.SO;
using UnityEngine;

namespace Lrw.Script.Upgrade
{
    public class UpgradeManager : AbstractManager
    {
        [SerializeField] private UpgradeTreeSO[] treeSoList;
        
        private Dictionary<string, UpgradeTree> treesDict;
        
        private const string UpgradeDataSaveName = "UpgradeData";
        
        public override void Initialize()
        {
            InitTrees();
        }

        public override void GameEnd()
        {
            base.GameEnd();
            SaveUpgradeData();
        }

        private void InitTrees()
        {
            treesDict = new();
            
            Dictionary<string, Dictionary<string, bool>> dict = GetSaveDict();
            
            HashSet<UpgradeTreeSO> hashSet = new();
            
            foreach (UpgradeTreeSO treeSo in treeSoList)
            {
                if(treeSo == null) continue;
                if (!hashSet.Add(treeSo)) continue;
                
                string treeName = treeSo.TreeName;
                Dictionary<string, bool> treeUpgradeData = dict.GetValueOrDefault(treeName, null);
                treesDict.Add(treeName, new UpgradeTree(treeSo,treeUpgradeData));
            }
        }
        
        private Dictionary<string, Dictionary<string, bool>> GetSaveDict()
        {
            Dictionary<string, JsonDict<string, bool>> loadDict = SaveSystem.Load(UpgradeDataSaveName,
                new JsonDict<string, JsonDict<string, bool>>())
                .ChangeDict();

            return loadDict.Keys.ToDictionary(upgradeTreeName => upgradeTreeName, upgradeTreeName => loadDict[upgradeTreeName].ChangeDict());
        }

        private void SaveUpgradeData()
        {
            Dictionary<string, JsonDict<string, bool>> a = new();

            foreach (UpgradeTree tree in treesDict.Values)
            {
                JsonDict<string, bool> jsonData = 
                    new(tree.Data.ToDictionary(x => x.Key.NodeName, x => x.Value));
                a.Add(tree.TreeSo.TreeName, jsonData);
            }
            
            JsonDict<string, JsonDict<string, bool>> saveData = new(a);
            
            bool saveComplete = SaveSystem.Save(UpgradeDataSaveName, saveData);

            if (!saveComplete)
            {
                FDebug.LogError("[UpgradeManager] Save failed");
            }
        }
        
        
        
    }
}