using System.Linq;
using KHO.Scripts.Eunms;
using KHO.Scripts.ItemSystem.Effects;
using UnityEngine;

namespace KHO.Scripts.ItemSystem
{
    [CreateAssetMenu(fileName = "Item", menuName = "KHO/Item", order = 0)]
    public class ItemSO : ScriptableObject
    {
        public const int MaxTagCount = 2;

        [field: SerializeField] public string ItemId { get; private set; }
        [field: SerializeField] public string ItemName { get; private set; }
        [field: SerializeField, TextArea] public string Description { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public Grade Grade { get; private set; }

        [field: SerializeField, Tooltip("최대 2개, 중복 불가. 태그 하나당 시너지 1로 센다.")] public Tags[] ItemTags { get; private set; } = new Tags[0];

        [field: SerializeField, Tooltip("획득 즉시 적용되는 상시 효과")] public ItemEffectSO[] Effects { get; private set; } = new ItemEffectSO[0];

        [field: SerializeField] public AcquireRoute AcquireRoute { get; private set; }
        [field: SerializeField] public int BuyPrice { get; private set; }
        [field: SerializeField] public int SellPrice { get; private set; }

        public int TagCount => ItemTags.Length;

        private void OnValidate()
        {
            if (ItemTags.Length > MaxTagCount)
                Debug.LogWarning($"[{name}] 태그는 최대 {MaxTagCount}개입니다. (현재 {ItemTags.Length}개)", this);

            if (ItemTags.Distinct().Count() != ItemTags.Length)
                Debug.LogWarning($"[{name}] 같은 태그가 중복되어 있습니다.", this);
        }
    }
}
