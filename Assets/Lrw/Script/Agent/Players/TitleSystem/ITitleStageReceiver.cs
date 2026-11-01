using Lrw.Script._Core.SceneSystem;

namespace Lrw.Script.Agent.Players.TitleSystem
{
    // 스테이지(씬)가 바뀌면 호출해서 그 스테이지의 칭호만 진행하게 한다.
    // 스테이지 변경 이벤트가 생기면 이벤트에서 ChangeStage를 호출하도록 연결한다.
    public interface ITitleStageReceiver
    {
        SceneSo CurrentStage { get; }
        void ChangeStage(SceneSo stage);
    }
}
