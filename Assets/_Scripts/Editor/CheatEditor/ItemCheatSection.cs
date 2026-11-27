using System.Linq;
using _Scripts.Agent.Players;
using _Scripts.Inventory;
using _Scripts.Item;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor.CheatEditor
{
    public class ItemCheatSection : ICheatSection
    {
        private readonly ItemSO[] _items = AssetDatabase.FindAssets($"t:{nameof(ItemSO)}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ItemSO>)
            .OrderBy(item => item.name)
            .ToArray();

        private readonly string[] _itemNames;

        private int _selectedIndex;
        private string _message;

        public ItemCheatSection()
        {
            _itemNames = _items
                .Select(item => string.IsNullOrEmpty(item.ItemName) ? item.name : $"{item.ItemName} ({item.name})")
                .ToArray();
        }

        public string Title => "아이템";

        public void Draw()
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            IInventoryModule inventory = player != null ? player.GetModule<IInventoryModule>() : null;
            if (inventory == null)
            {
                EditorGUILayout.LabelField("인벤토리를 가진 플레이어가 없습니다.");
                return;
            }

            if (_items.Length == 0)
            {
                EditorGUILayout.LabelField("ItemSO 에셋이 없습니다.");
                return;
            }

            _selectedIndex = EditorGUILayout.Popup("아이템", _selectedIndex, _itemNames);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("지급"))
                    _message = inventory.TryAddItem(_items[_selectedIndex]) ? null : "지급 실패 (인벤토리가 잠겼거나 가득 참)";
                if (GUILayout.Button("장착 칸 확장"))
                    _message = inventory.TryExpandEquipment() ? null : "확장 실패 (잠겼거나 이미 최대)";
                if (GUILayout.Button("모두 비우기"))
                {
                    inventory.Clear();
                    _message = null;
                }
            }

            if (!string.IsNullOrEmpty(_message))
                EditorGUILayout.HelpBox(_message, MessageType.Warning);
        }

        public void Tick() { }

        public void Release() { }
    }
}
