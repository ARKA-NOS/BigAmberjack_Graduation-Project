using System.Linq;
using _Scripts._Core._Debug;
using _Scripts._Core._Manager;
using _Scripts._Core._SaveSystem;
using _Scripts._Core._ServiceLocator;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Upgrade;
using _Scripts.Upgrade.SO;
using UnityEngine;

namespace _Scripts.Agent.Players.CharacterSystem
{
    public class CharacterManager : AbstractManager, ICharacterManager
    {
        [SerializeField] private CharacterData defaultCharacter;
        [SerializeField] private CharacterData[] characterList;

        private CharacterData currentCharacter;

        private UpgradeTree _activeTree;
        private ModuleOwner _activeOwner;

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
            UnbindUpgradeTree();
            ServiceLocator.Register<ICharacterManager>(new NullCharacterManager());
            SaveCurrentCharacter();
        }

        private CharacterData LoadCurrentCharacter()
        {
            CharacterSaveData loadData = SaveSystem.Load(CharacterSaveName,
                new CharacterSaveData(DefaultValue));
            
            CharacterData data = characterList.FirstOrDefault(x => x.name == loadData.saveData)
                                 ?? defaultCharacter;
            
            return data;
        }

        private void SaveCurrentCharacter()
        {
            SaveSystem.Save(CharacterSaveName,
                new CharacterSaveData(currentCharacter?.name ?? DefaultValue));
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

            if (currentCharacter.TreeSo == null)
            {
                FDebug.LogError("[CharacterManager] currentCharacter.TreeSo is null");
                return;
            }
            
            UpgradeTree tree = ServiceLocator.Get<IUpgradeManager>().GetUpgradeTree(currentCharacter.TreeSo.TreeName);

            if (tree != null)
            {
                tree.UpgradeOwner(owner);
                BindUpgradeTree(tree, owner);
            }
            else
            {
                FDebug.LogWarning("[CharacterManager] currentCharacter UpgradeTree is not found");
            }

            owner.transform.position = pos;
        }

        private void BindUpgradeTree(UpgradeTree tree, ModuleOwner owner)
        {
            UnbindUpgradeTree();
            _activeTree = tree;
            _activeOwner = owner;
            _activeTree.OnUpgradeChanged += HandleUpgradeChanged;
        }

        private void UnbindUpgradeTree()
        {
            if (_activeTree != null)
                _activeTree.OnUpgradeChanged -= HandleUpgradeChanged;
            _activeTree = null;
            _activeOwner = null;
        }

        private void HandleUpgradeChanged(UpgradeNodeSO node)
        {
            if (_activeOwner == null)
            {
                UnbindUpgradeTree();
                return;
            }

            if (node == null || node.UpgradeData == null) return;
            if (!_activeTree.IsUpgrade(node)) return;

            node.UpgradeData.AgentUpgrade(_activeOwner);
        }

    }
}