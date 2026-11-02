using Lrw.Script._Core._EventSystem;
using UnityEngine;

namespace Lrw.Script.UI.PlayerUI.InteractUI_system
{
    public static class InteractUIEvent
    {
        public static readonly InteractUISettingEvent InteractUISettingEvent = new();
    }

    public class InteractUISettingEvent : IEvent
    {
        public bool Active = true;
        public Vector3 WorldPos;

        public InteractUISettingEvent Init(bool active, Vector3 worldPos)
        {
            Active = active;
            WorldPos = worldPos;
            return this;
        }
    }
}