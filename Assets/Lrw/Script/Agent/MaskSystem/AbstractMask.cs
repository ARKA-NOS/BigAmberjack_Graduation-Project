using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.MaskSystem
{
    public abstract class AbstractMask : MonoBehaviour,IMask
    {
        [field:SerializeField] public MaskSo MaskSo { get; private set; }
        public abstract void Init(ModuleOwner owner);
        public abstract void Enter();
        public abstract void Update();
        public abstract void Exit();
    }
}