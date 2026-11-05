using _Scripts._Core._EventSystem;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.InteractUI_system
{
    [RequireComponent(typeof(Canvas))]
    public class InteractUI : MonoBehaviour
    {
        [SerializeField] private Transform interactUI;
        [SerializeField] private Vector2 uiOffset;
        
        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();

            EventBus<InteractUISettingEvent>.Subscribe(UISetting);
        }

        private void OnDestroy()
        {
            EventBus<InteractUISettingEvent>.UnSubscribe(UISetting);
        }
        
        private void Update()
        {
            _canvas.worldCamera = Camera.main;
        }
        
        private void UISetting(InteractUISettingEvent evt)
        {
            if(interactUI == null) return;
            interactUI.gameObject.SetActive(evt.Active);
            interactUI.position = evt.WorldPos + (Vector3)uiOffset;
        }
        
    }
}