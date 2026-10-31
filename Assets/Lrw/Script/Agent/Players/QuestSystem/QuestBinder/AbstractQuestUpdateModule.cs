using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem.QuestBinder
{
    // 퀘스트 진행도는 이 클래스의 하위 클래스만 바꾼다.
    // 게임 모듈은 EventBus로 이벤트만 내고, 하위 클래스가 OnInit에서 그 이벤트를 구독해 진행도를 올린다.
    public abstract class AbstractQuestUpdateModule : MonoBehaviour
    {
        [SerializeField] private QuestKey questKey;

        protected ModuleOwner Owner { get; private set; }
        private IQuestModule  _questModule;
        private bool _isInitialized;
        protected bool IsInitialized => _isInitialized;

        public void Init(ModuleOwner owner)
        {
            FDebug.Assert(questKey != null, $"[{GetType().Name} : {name}] questKey is null");

            Owner = owner;
            _questModule = Owner.GetModule<IQuestModule>();
            _isInitialized = true;
            OnInit(owner);
        }

        protected abstract void OnInit(ModuleOwner owner);

        // OnInit에서 구독한 이벤트를 여기서 해제한다.
        protected virtual void OnRelease() { }

        protected virtual void OnDestroy()
        {
            if (!_isInitialized) return;
            _isInitialized = false;
            OnRelease();
        }

        protected void AddQuestAmount(float amount)
        {
            _questModule.AddQuestAmount(questKey, amount);
        }

        protected void SetQuestAmount(float amount)
        {
            _questModule.SetQuestAmount(questKey, amount);
        }

        protected void ResetQuestAmount() => SetQuestAmount(0f);

    }
}
