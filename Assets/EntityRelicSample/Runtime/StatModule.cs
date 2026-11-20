#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    MoveSpeed,
    Attack
}

[Serializable]
public sealed class StatModule : EntityModule
{
    [SerializeField, Min(0f)] private float baseMoveSpeed = 2f;
    [SerializeField, Min(0f)] private float baseAttack = 10f;

    private readonly Dictionary<object, (StatType Type, float Amount)> modifiers = new();

    public float GetStatValue(StatType statType)
    {
        var value = statType switch
        {
            StatType.MoveSpeed => baseMoveSpeed,
            StatType.Attack => baseAttack,
            _ => throw new ArgumentOutOfRangeException(nameof(statType), statType, "Unsupported stat type.")
        };

        foreach (var modifier in modifiers.Values)
        {
            if (modifier.Type == statType)
                value += modifier.Amount;
        }

        return Mathf.Max(0f, value);
    }

    public void AddModifier(StatType statType, object source, float amount)
    {
        modifiers[source] = (statType, amount);
    }

    public void RemoveModifier(object source)
    {
        modifiers.Remove(source);
    }

    public override void Release()
    {
        modifiers.Clear();
    }
}