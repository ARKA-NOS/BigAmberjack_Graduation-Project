using System;

namespace Lrw.Script.Player.CharacterSystem
{
    [Serializable]
    public struct CharacterSaveData
    {
        public string saveData;

        public CharacterSaveData(string data)
        {
            saveData = data;
        }
    }
}