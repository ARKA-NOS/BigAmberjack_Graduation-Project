using System.Collections;
using System.Linq;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies.Managers;
using _Scripts.Agent.Players;
using _Scripts.Agent.Players.Modules;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.UI.UI_System
{
    // 열려 있는 동안 게임 시간을 멈추고(TimeStopService) 플레이어 스킬 입력(공격·스킬·대시)을 끄는 창.
    // 시간이 멈춰도 페이드가 진행되도록 unscaled 시간으로 직접 페이드한다.
    // 닫혀 있는 동안은 레이캐스트를 막지 않아서, 투명해진 창이 뒤에 있는 다른 UI의 클릭을 가로채지 않게 한다.
    // 열고 닫을 때 할 일이 더 있으면 OnOpened/OnClosed를 오버라이드한다.
    public abstract class PausingWindow : AbstractWindow
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeSpeed = 3f;

        private ICanInputOnOff _blockedSkillInput;
        private Coroutine _fade;

        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            canvasGroup.alpha = 0f;
            SetInteractable(false);
        }

        public sealed override void Open(object data)
        {
            IsOpen = true;
            StartFade(1f);
            SetInteractable(true);
            Freeze();
            BlockSkillInput();
            OnOpened(data);
        }

        // 페이드아웃이 끝나기 전에 바로 입력을 끊고 시간을 되돌린다.
        public sealed override void Close()
        {
            IsOpen = false;
            StartFade(0f);
            SetInteractable(false);
            Unfreeze();
            RestoreSkillInput();
            OnClosed();
        }

        protected virtual void OnOpened(object data) { }

        protected virtual void OnClosed() { }

        protected virtual void OnDisable()
        {
            Unfreeze();
            RestoreSkillInput();
        }

        private void SetInteractable(bool value)
        {
            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts = value;
        }

        // 일시정지를 요청한 모두가 풀어야 시간이 다시 흐른다. 같은 요청을 여러 번 해도 괜찮다.
        private void Freeze() => ServiceLocator.Get<TimeStopService>()?.SetPaused(this, true);

        private void Unfreeze() => ServiceLocator.Get<TimeStopService>()?.SetPaused(this, false);

        private void StartFade(float target)
        {
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(Fade(target));
        }

        private IEnumerator Fade(float target)
        {
            while (!Mathf.Approximately(canvasGroup.alpha, target))
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.unscaledDeltaTime);
                yield return null;
            }

            _fade = null;
        }

        private void BlockSkillInput()
        {
            if (_blockedSkillInput != null) return;
            if (ServiceLocator.Get<IPlayerController>() is not ModuleOwner player) return;

            _blockedSkillInput = player.GetModules<ICanInputOnOff>().FirstOrDefault(module => module is IPlayerSkillModule);
            _blockedSkillInput?.SetActive(false);
        }

        // 이 창이 끈 입력만 되돌린다. 열린 적이 없으면 아무것도 켜지 않는다.
        private void RestoreSkillInput()
        {
            if (_blockedSkillInput is Object module && module != null)
                _blockedSkillInput.SetActive(true);

            _blockedSkillInput = null;
        }
    }
}
