using UnityEngine;

namespace Lrw.Script.UI.AlarmUI
{
    public struct AlarmData
    {
        public string Title;
        public string Message;
        public Sprite Image;

        public AlarmData(string title, string message, Sprite image)
        {
            Title = title;
            Message = message;
            Image = image;
        }
        
    }
}