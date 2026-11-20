using _Scripts.Agent.SkillSystem;
using _Scripts.Agent.SkillSystem.Interface;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Ticking;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.HUD_UI.Skill_UI
{
    // 스킬 슬롯 하나(아이콘, 쿨타임, 사용 불가 표시)의 ViewModel. 슬롯마다 에셋을 따로 만든다(스킬 1, 스킬 2).
    // 쿨타임과 장착 스킬은 이벤트 없이 바뀌므로 Tick에서 매 프레임 읽는다.
    [CreateAssetMenu(fileName = "Skill Slot View Model", menuName = "UI/MVVM/Skill Slot View Model")]
    public class SkillSlotViewModel : ViewModelBase<SkillSlotSource>, ITickableViewModel
    {
        private readonly Observable<Sprite> _icon = new();
        private readonly Observable<bool> _hasCoolTime = new();
        private readonly Observable<float> _coolTimeRatio = new();
        private readonly Observable<bool> _blocked = new();

        private SkillSlotSource _source;
        private bool _hasSource;

        // 슬롯에 장착된 스킬 아이콘. 비어 있으면 null.
        public IReadOnlyObservable<Sprite> Icon => _icon;

        // 쿨타임이 있는 스킬인지.
        public IReadOnlyObservable<bool> HasCoolTime => _hasCoolTime;

        // 남은 쿨타임 비율. 1이면 막 사용함, 0이면 쿨타임 끝.
        public IReadOnlyObservable<float> CoolTimeRatio => _coolTimeRatio;

        // 쿨타임이 아닌데도 지금 쓸 수 없는 상태(다른 스킬 사용 중, 조건 미충족 등).
        public IReadOnlyObservable<bool> Blocked => _blocked;

        protected override void OnConnect(SkillSlotSource source)
        {
            _source = source;
            _hasSource = source.Module != null;
            Tick();
        }

        protected override void OnDisconnect()
        {
            _source = default;
            _hasSource = false;
        }

        protected override void ResetState()
        {
            _icon.Value = null;
            _hasCoolTime.Value = false;
            _coolTimeRatio.Value = 0f;
            _blocked.Value = false;
        }

        public void Tick()
        {
            if (!_hasSource) return;

            ISkill skill = _source.Module.GetSlotSkill(_source.Slot);
            SkillSO skillSo = skill?.SkillSo;
            _icon.Value = skillSo != null ? skillSo.SkillImage : null;

            IHasCoolTime coolTime = skill as IHasCoolTime;
            _hasCoolTime.Value = coolTime != null;
            _coolTimeRatio.Value = coolTime != null && coolTime.CoolTime > 0f
                ? coolTime.RemainingCoolTime / coolTime.CoolTime
                : 0f;

            bool canUse = skill != null && skill.CanUseSkill();
            bool coolTimeDone = coolTime == null || Mathf.Approximately(coolTime.RemainingCoolTime, 0f);
            _blocked.Value = !canUse && coolTimeDone;
        }
    }
}
