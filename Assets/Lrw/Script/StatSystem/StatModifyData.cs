namespace Lrw.Script.StatSystem
{
    public readonly struct StatModifyData
    {
        public readonly int Priority;
        private readonly float _value;
        private readonly ModifyMathType _type;

        public StatModifyData(int priority, float value, ModifyMathType type)
        {
            Priority = priority;
            _value = value;
            _type = type;
        }

        public float GetValue(float originValue)
        {
            return _type switch
            {
                ModifyMathType.Add => originValue + _value,
                ModifyMathType.Multiply => originValue * _value,
                _ => originValue
            };
        }
    }
}