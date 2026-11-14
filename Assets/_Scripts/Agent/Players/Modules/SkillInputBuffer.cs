using _Scripts.Agent.SkillSystem;

namespace _Scripts.Agent.Players.Modules
{
    public sealed class SkillInputBuffer
    {
        private readonly float _bufferTime;
        private SkillSO _skill;
        private float _expireTime;

        public SkillInputBuffer(float bufferTime)
        {
            _bufferTime = bufferTime;
        }

        public void Store(SkillSO skill, float now)
        {
            _skill = skill;
            _expireTime = now + _bufferTime;
        }

        public bool TryGet(float now, out SkillSO skill)
        {
            if (_skill != null && now > _expireTime)
                _skill = null;

            skill = _skill;
            return skill != null;
        }

        public void Clear() => _skill = null;
    }
}
