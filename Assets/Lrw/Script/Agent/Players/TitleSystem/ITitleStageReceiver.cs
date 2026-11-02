using SDW.Scripts.Maps.Data;

namespace Lrw.Script.Agent.Players.TitleSystem
{
    // 맵(방)에 입장하면 호출해서 그 맵의 칭호만 진행하게 한다.
    // MapEnteredEvent가 넘겨 주는 RoomDataSO로 ChangeStage를 호출한다.
    public interface ITitleStageReceiver
    {
        RoomDataSO CurrentStage { get; }
        void ChangeStage(RoomDataSO stage);
    }
}
