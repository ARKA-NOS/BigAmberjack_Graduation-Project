#nullable enable

using System;
using UnityEngine;

[Serializable]
public sealed class ScaleEquipLogic : EquipLogic
{
    [SerializeField, Min(0.01f)] private float multiplier = 1.25f;

    private Transform? target;
    private Vector3 originalScale;

    public override void OnEquip(EntityBase owner)
    {
        var entityTransform = owner.transform;
        target = entityTransform;
        originalScale = entityTransform.localScale;
        entityTransform.localScale = originalScale * multiplier;
        Debug.Log($"[Relic] Equip scale: {entityTransform.localScale}", owner);
    }

    public override void OnUnequip()
    {
        var entityTransform = target;
        target = null;

        if (entityTransform == null)
            return;

        entityTransform.localScale = originalScale;
        Debug.Log($"[Relic] Unequip scale: {entityTransform.localScale}", entityTransform);
    }
}