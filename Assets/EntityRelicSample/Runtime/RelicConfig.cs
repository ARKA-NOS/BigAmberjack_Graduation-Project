#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Entity Relic Sample/Relic")]
public sealed class RelicConfig : ScriptableObject
{
    [SerializeReference, SubclassSelector]
    private EquipLogic?[] logics = Array.Empty<EquipLogic?>();

    internal IReadOnlyList<EquipLogic?> Logics => logics;
}