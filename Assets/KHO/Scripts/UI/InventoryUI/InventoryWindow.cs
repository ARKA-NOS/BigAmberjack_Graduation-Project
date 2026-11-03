using System.Linq;
using DevLib.ModuleSystem;
using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Agent.Players;
using Lrw.Script.Agent.Players.Modules;
using Lrw.Script.UI.Template;

namespace KHO.Scripts.UI.InventoryUI
{
    // 창이 열려 있는 동안 플레이어 스킬 입력(공격·스킬·대시)을 끈다. 이동·점프는 그대로 둔다.
    public class InventoryWindow : FadeWindow
    {
        private ICanInputOnOff _blockedSkillInput;

        public override void Open(object data)
        {
            base.Open(data);
            BlockSkillInput();
        }

        public override void Close()
        {
            base.Close();
            RestoreSkillInput();
        }

        private void OnDisable()
        {
            RestoreSkillInput();
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
