using _Scripts._Core._EventSystem;
using _Scripts.Agent.Players.InteractSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Inventory;
using _Scripts.Item.ItemTooltip;
using _Scripts.UI.PlayerUI.InteractUI_system;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.Item
{
    // 바닥에 떨어진 아이템. 상호작용 키(F)를 짧게 누르면 줍고, 꾹 누르고 있으면 파괴한다.
    // InteractModule은 누른 순간에만 Interact를 부르므로, 뗐는지/누르고 있는지는 holdKey로 따로 읽는다.
    // 상호작용 대상이 되거나 벗어나면 ItemTooltipEvents로 알린다. (툴팁 UI는 모른다)
    public class ItemPickup : AbstractInteractObject, IItemTooltipSource
    {
        private const float FocusTolerance = 0.01f;
        private const float RemoveDelay = 0.1f;

        [SerializeField] private ItemSO item;
        [SerializeField] private SpriteRenderer iconRenderer;

        [Header("Tooltip")]
        [SerializeField] private Vector2 tooltipOffset = new(0f, 0.5f);

        [Header("Input")]
        [SerializeField, Tooltip("상호작용 키와 같은 키로 맞춘다.")]
        private InputAction holdKey = new InputAction("Item Hold", InputActionType.Button, "<Keyboard>/f");
        [SerializeField, Tooltip("이 시간 안에 떼면 줍는다.")] private float pickupMaxPressTime = 0.25f;
        [SerializeField, Tooltip("이 시간 동안 누르고 있으면 파괴한다.")] private float destroyHoldTime = 1f;

        [Header("Destroy")]
        [SerializeField, Tooltip("파괴하면 아이템 판매가(SellPrice)만큼 재화를 코인으로 떨어뜨린다.")]
        private ItemCoinDropSO destroyReward;

        private ModuleOwner _holder;
        private float _holdTime;
        private bool _isFocused;

        public ItemSO Item => item;
        public Vector2 TooltipPosition => Position + tooltipOffset;
        public float DestroyProgress => _holder == null ? 0f : Mathf.Clamp01(_holdTime / destroyHoldTime);

        // 코드로 생성한 픽업(상자 보상 등)에 아이템을 지정한다. Start보다 먼저 호출해야 한다.
        public void Init(ItemSO newItem)
        {
            item = newItem;
            if (iconRenderer != null && item != null)
                iconRenderer.sprite = item.Icon;
        }

        private void Start()
        {
            if (iconRenderer != null && item != null)
                iconRenderer.sprite = item.Icon;
        }

        private void OnEnable()
        {
            holdKey.Enable();
            EventBus<InteractUISettingEvent>.Subscribe(HandleInteractTarget);
        }

        private void OnDisable()
        {
            holdKey.Disable();
            EventBus<InteractUISettingEvent>.UnSubscribe(HandleInteractTarget);
            SetFocused(false);
        }

        public override void Interact(ModuleOwner owner)
        {
            if (_holder != null) return;

            _holder = owner;
            _holdTime = 0f;
        }

        private void Update()
        {
            if (_holder == null) return;

            // 누르는 도중 대상에서 벗어나면 취소한다.
            if (!_isFocused)
            {
                CancelHold();
                return;
            }

            _holdTime += Time.deltaTime;

            if (holdKey.IsPressed())
            {
                if (_holdTime >= destroyHoldTime)
                    DestroyItem();
                return;
            }

            if (_holdTime <= pickupMaxPressTime)
                PickUp(_holder);

            CancelHold();
        }

        private void PickUp(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IInventoryModule inventory)) return;

            if (inventory.TryAddItem(item))
                Remove();
        }

        private void DestroyItem()
        {
            if (destroyReward != null && item != null)
                destroyReward.Drop(item.SellPrice, transform.position);

            Remove();
        }

        // InteractModule은 FixedUpdate에서만 대상을 다시 찾아서, 바로 파괴하면 그 사이 Update에서 파괴된 대상의 Position을 읽는다.
        // 먼저 꺼서 대상에서 빠지게 하고 FixedUpdate가 몇 번 돈 뒤 파괴한다.
        private void Remove()
        {
            CancelHold();
            gameObject.SetActive(false);
            Destroy(gameObject, RemoveDelay);
        }

        private void CancelHold()
        {
            _holder = null;
            _holdTime = 0f;
        }

        // InteractModule은 대상의 Position만 알려 주므로, 그 위치가 내 위치와 같으면 내가 대상이다.
        private void HandleInteractTarget(InteractUISettingEvent evt)
        {
            bool focused = evt.Active && ((Vector2)evt.WorldPos - Position).sqrMagnitude < FocusTolerance * FocusTolerance;
            SetFocused(focused);
        }

        private void SetFocused(bool focused)
        {
            if (_isFocused == focused) return;
            _isFocused = focused;

            if (focused)
                ItemTooltipEvents.Focus(this);
            else
                ItemTooltipEvents.Unfocus(this);
        }
    }
}
