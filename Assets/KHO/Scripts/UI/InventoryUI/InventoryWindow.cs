using System.Collections;
using System.Linq;
using DevLib.ModuleSystem;
using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Agent.Players;
using Lrw.Script.Agent.Players.Modules;
using Lrw.Script.UI;
using UnityEngine;

namespace KHO.Scripts.UI.InventoryUI
{
    // 창이 열려 있는 동안 게임 시간을 멈추고(Time.timeScale = 0) 플레이어 스킬 입력(공격·스킬·대시)을 끈다.
    // 시간이 멈춰도 페이드가 진행되도록 FadeWindow 대신 unscaled 시간으로 직접 페이드한다.
    // 닫혀 있는 동안은 레이캐스트를 막지 않아서, 투명해진 창이 뒤에 있는 다른 UI의 클릭을 가로채지 않게 한다.
    public class InventoryWindow : AbstractWindow
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeSpeed = 3f;

        private ICanInputOnOff _blockedSkillInput;
        private Coroutine _fade;
        private float _prevTimeScale = 1f;
        private bool _isFrozen;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            SetInteractable(false);
        }

        public override void Open(object data)
        {
            StartFade(1f);
            SetInteractable(true);
            Freeze();
            BlockSkillInput();
        }

        // 페이드아웃이 끝나기 전에 바로 입력을 끊고 시간을 되돌린다.
        public override void Close()
        {
            StartFade(0f);
            SetInteractable(false);
            Unfreeze();
            RestoreSkillInput();
        }

        private void OnDisable()
        {
            Unfreeze();
            RestoreSkillInput();
        }

        private void SetInteractable(bool value)
        {
            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts = value;
        }

        // 열기 전 시간 배율을 기억해 두었다가 닫을 때 그대로 되돌린다.
        private void Freeze()
        {
            if (_isFrozen) return;

            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _isFrozen = true;
        }

        private void Unfreeze()
        {
            if (!_isFrozen) return;

            Time.timeScale = _prevTimeScale;
            _isFrozen = false;
        }

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
            if (_blockedSkillInput is UnityEngine.Object module && module != null)
                _blockedSkillInput.SetActive(true);

            _blockedSkillInput = null;
        }
    }
}
