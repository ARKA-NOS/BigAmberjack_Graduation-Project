using System;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players;
using _Scripts.Maps;
using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // LinearStageGenerator와 서비스 로케이터의 플레이어를 IMinimapSource로 감싼다. MinimapConnector가 만든다.
    public class StageMinimapSource : IMinimapSource
    {
        private readonly LinearStageGenerator _stage;

        public StageMinimapSource(LinearStageGenerator stage)
        {
            _stage = stage;
        }

        public RoomController CurrentRoom => _stage != null ? _stage.CurrentRoom : null;

        // 매 프레임 불리므로 씬 검색 대신 서비스 로케이터에서 가져온다. 플레이어가 없으면 Null 객체가 등록되어 있어 null이 된다.
        public Transform Player =>
            ServiceLocator.Get<IPlayerController>() is Component player && player != null ? player.transform : null;

        public event Action<RoomController> RoomEntered
        {
            add
            {
                if (_stage != null) _stage.RoomEntered += value;
            }
            remove
            {
                if (_stage != null) _stage.RoomEntered -= value;
            }
        }
    }
}
