#nullable enable

using System;
using UnityEngine;

[Serializable]
public sealed class StatEquipLogic : EquipLogic
{
    [SerializeField] private StatType statType = StatType.MoveSpeed;
    [SerializeField] private float amount = 2f;

    private StatModule? statModule;

    public override void OnEquip(EntityBase owner)
    {
        if (!owner.TryGetModule<StatModule>(out var module))
            return;

        module.AddModifier(statType, this, amount);
        statModule = module;
        Debug.Log($"[Relic] Equip {statType}: {module.GetStatValue(statType):0.##}", owner);
    }

    public override void OnUnequip()
    {
        var module = statModule;
        if (module == null)
            return;

        module.RemoveModifier(this);
        statModule = null;
        Debug.Log($"[Relic] Unequip {statType}: {module.GetStatValue(statType):0.##}");
    }
}