#nullable enable

using System;
using UnityEngine;

public sealed class EntityManager : MonoBehaviour
{
    [SerializeField] private EntityBase player = null!;

    private void Awake()
    {
        if (player == null)
            throw new InvalidOperationException("Assign a player to EntityManager.");

        player.PreInit();
    }

    [ContextMenu("Init Player")]
    public void InitPlayer()
    {
        player.Init();
    }

    [ContextMenu("Release Player")]
    public void ReleasePlayer()
    {
        player.Release();
    }
}