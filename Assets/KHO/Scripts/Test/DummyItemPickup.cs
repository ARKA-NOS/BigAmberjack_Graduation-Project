using DevLib.ModuleSystem;
using KHO.Scripts.Eunms;
using KHO.Scripts.InventorySystem;
using KHO.Scripts.ItemSystem;
using Lrw.Script.Player.InteractSystem;
using UnityEngine;

namespace KHO.Scripts.Test
{
    // 에셋 없이 빈 오브젝트에 붙이기만 하면 F로 먹을 수 있는 임시 아이템.
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public class DummyItemPickup : AbstractInteractObject
    {
        // InteractModule의 interactLayer가 MapObject만 감지한다.
        private const string InteractLayerName = "MapObject";

        [SerializeField] private string itemName = "더미 아이템";
        [SerializeField] private Grade grade = Grade.Normal;
        [SerializeField] private Tags[] tags = { Tags.Bleeding };
        [SerializeField] private Color color = Color.yellow;

        private static Sprite _squareSprite;

        private ItemSO _item;

        private void Awake()
        {
            gameObject.layer = LayerMask.NameToLayer(InteractLayerName);
            GetComponent<BoxCollider2D>().isTrigger = true;

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite == null)
                spriteRenderer.sprite = GetSquareSprite();
            spriteRenderer.color = color;

            _item = ItemSO.CreateRuntime($"dummy_{itemName}", itemName, spriteRenderer.sprite, grade, tags);
        }

        public override void Interact(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IInventoryModule inventory))
            {
                Debug.LogWarning($"[{owner.name}] InventoryModule이 없어 아이템을 먹을 수 없습니다.", this);
                return;
            }

            if (!inventory.TryAddItem(_item))
            {
                Debug.Log("인벤토리가 가득 찼거나 잠겨 있습니다.", this);
                return;
            }

            Debug.Log($"{_item.ItemName} 획득", this);
            Destroy(gameObject);
        }

        private static Sprite GetSquareSprite()
        {
            if (_squareSprite != null) return _squareSprite;

            Texture2D texture = Texture2D.whiteTexture;
            _squareSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            return _squareSprite;
        }
    }
}
