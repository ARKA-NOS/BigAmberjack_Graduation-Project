using UnityEngine;

namespace _Scripts.Agent.Players.Skills
{
    public sealed class MeleeComboCounter
    {
        private readonly int _comboLength;
        private float _expireTime = float.NegativeInfinity;

        public int Current { get; private set; }

        public MeleeComboCounter(int comboLength)
        {
            _comboLength = Mathf.Max(1, comboLength);
        }

        public int Begin(float now)
        {
            if (Current >= _comboLength || now >= _expireTime)
                Current = 0;
            return Current;
        }

        public void Advance(float expireTime)
        {
            Current++;
            _expireTime = expireTime;
        }
    }
}
