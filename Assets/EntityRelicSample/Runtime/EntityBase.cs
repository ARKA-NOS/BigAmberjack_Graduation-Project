#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class EntityBase : MonoBehaviour
{
    [SerializeReference, SubclassSelector]
    private EntityModule?[] modules = Array.Empty<EntityModule?>();

    private readonly Dictionary<Type, EntityModule> modulesByType = new();

    public Rigidbody2D Rigidbody { get; private set; } = null!;

    [ContextMenu("PreInit Entity")]
    public void PreInit()
    {
        Rigidbody = GetComponent<Rigidbody2D>();

        foreach (var module in modules)
        {
            if (module != null)
                modulesByType.Add(module.GetType(), module);
        }

        foreach (var module in modules)
            module?.PreInit(this);
    }

    [ContextMenu("Init Entity")]
    public void Init()
    {
        foreach (var module in modules)
            module?.Init();

        gameObject.SetActive(true);

        foreach (var module in modules)
            module?.OnActivated();
    }

    [ContextMenu("Release Entity")]
    public void Release()
    {
        for (var i = modules.Length - 1; i >= 0; i--)
            modules[i]?.Release();

        gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        foreach (var module in modules)
            module?.OnFixedUpdate();
    }

    public bool TryGetModule<T>([NotNullWhen(true)] out T? module) where T : EntityModule
    {
        modulesByType.TryGetValue(typeof(T), out var registered);
        module = registered as T;
        return module != null;
    }
}