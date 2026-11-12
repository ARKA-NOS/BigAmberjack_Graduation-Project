#nullable enable

using System;

[Serializable]
public abstract class EquipLogic
{
    public abstract void OnEquip(EntityBase owner);

    public abstract void OnUnequip();
}