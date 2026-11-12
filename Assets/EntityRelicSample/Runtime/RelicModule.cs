#nullable enable

using System;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public sealed class RelicModule : EntityModule
{
    [SerializeField] private RelicConfig? initialRelic = null;

    private EntityBase? owner;
    private RelicConfig? runtimeRelic;

    public RelicConfig? EquippedRelic { get; private set; }

    public override void PreInit(EntityBase entity)
    {
        owner = entity;
    }

    public override void OnActivated()
    {
        var relic = initialRelic;
        if (relic != null)
            Equip(relic);
    }

    public void Equip(RelicConfig relic)
    {
        var entity = owner;
        if (entity == null || !entity.isActiveAndEnabled || relic == null || EquippedRelic == relic)
            return;

        Unequip();

        var instance = Object.Instantiate(relic);
        runtimeRelic = instance;
        EquippedRelic = relic;

        try
        {
            foreach (var logic in instance.Logics)
                logic?.OnEquip(entity);
        }
        catch
        {
            Unequip();
            throw;
        }
    }

    public void Unequip()
    {
        var instance = runtimeRelic;
        if (instance == null)
            return;

        runtimeRelic = null;
        EquippedRelic = null;

        try
        {
            for (var i = instance.Logics.Count - 1; i >= 0; i--)
                instance.Logics[i]?.OnUnequip();
        }
        finally
        {
            Object.Destroy(instance);
        }
    }

    public override void Release()
    {
        Unequip();
    }
}