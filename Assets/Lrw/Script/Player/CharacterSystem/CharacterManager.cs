using System.Linq;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script._Core._Manager;
using Lrw.Script._Core._SaveSystem;
using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Upgrade;
using UnityEngine;

namespace Lrw.Script.Player.CharacterSystem
{
    public class CharacterManager : AbstractManager, ICharacterManager
    {
        [SerializeField] private CharacterData defaultCharacter;
        [SerializeField] private CharacterData[] characterList;

        private CharacterData currentCharacter;
        
        private const string CharacterSaveName = "CharacterSaveData";
        private const string DefaultValue = "Default";
        
        public override void Initialize()
        {
            currentCharacter = LoadCurrentCharacter();
            ServiceLocator.Register<ICharacterManager>(this);
        }

        public override void GameEnd()
        {
            base.GameEnd();
            ServiceLocator.Register<ICharacterManager>(new NullCharacterManager());
            SaveCurrentCharacter();
        }

        private CharacterData LoadCurrentCharacter()
        {
            string loadStr = SaveSystem.Load(CharacterSaveName,DefaultValue);
            
            CharacterData data = characterList.FirstOrDefault(x => x.name == loadStr) ?? defaultCharacter;
            
            return data;
        }

        private void SaveCurrentCharacter()
        {
            SaveSystem.Save(CharacterSaveName,currentCharacter?.name ?? DefaultValue);
        }

        public void CreateCharacter(Vector2 pos)
        {
            if (currentCharacter == null)
            {
                FDebug.LogError("[CharacterManager] currentCharacter is null");
                return;
            }
            GameObject go = Instantiate(currentCharacter.Prefab);

            ModuleOwner owner = go.GetComponentInChildren<ModuleOwner>();

            if (owner == null)
            {
                FDebug.LogError("[CharacterManager] ModuleOwner is not found");
                return;
            }
            
            UpgradeTree tree = ServiceLocator.Get<IUpgradeManager>().GetUpgradeTree(currentCharacter.TreeSo.TreeName);

            if (tree != null)
            {
                tree.UpgradeOwner(owner);
            }
            else
            {
                FDebug.LogWarning("[CharacterManager] currentCharacter UpgradeTree is not found");
            }
            
            
            
            owner.transform.position = pos;
        }
        
    }
}