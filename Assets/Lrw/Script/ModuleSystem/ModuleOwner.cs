using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script.ModuleSystem
{
    public abstract class ModuleOwner : MonoBehaviour
    {
        private Dictionary<Type, IModule> _moduleDict;
        protected virtual void Awake()
        {
            _moduleDict = GetComponentsInChildren<IModule>().ToDictionary(module => module.GetType());
            InitializeModules();
            AfterInitializeModules();

        }

        protected virtual void AfterInitializeModules()
        {
            foreach (IModule module in _moduleDict.Values)
            {
                module.Initialize(this);
            }
        }

        protected virtual void InitializeModules()
        {
            foreach (IModule module in _moduleDict.Values)
            {
                module.AfterInitialize();
            }
        }

        public T GetModule<T>()
        {
            if(_moduleDict.TryGetValue(typeof(T),out IModule module))
                return (T)module;
            IModule findModule = _moduleDict.Values.FirstOrDefault(m => m is T);
            
            if(findModule is T castedModule)
                return castedModule;
            
            return default(T);
        }
    }
}