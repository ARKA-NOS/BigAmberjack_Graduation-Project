using System;

namespace Lrw.Script.Agent.Players.CharacterSystem
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