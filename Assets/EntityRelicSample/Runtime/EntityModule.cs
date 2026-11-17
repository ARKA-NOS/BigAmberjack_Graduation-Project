#nullable enable

using System;

[Serializable]
public abstract class EntityModule
{
    public virtual void PreInit(EntityBase owner) { }

    public virtual void Init() { }

    public virtual void OnActivated() { }

    public virtual void OnFixedUpdate() { }

    public virtual void Release() { }
}