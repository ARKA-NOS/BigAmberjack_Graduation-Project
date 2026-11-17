#nullable enable

using System;
using UnityEngine;

[Serializable]
public sealed class MoveModule : EntityModule
{
    private Rigidbody2D? body;
    private StatModule? statModule;
    private Vector2 direction;

    public override void PreInit(EntityBase owner)
    {
        body = owner.Rigidbody;
        owner.TryGetModule<StatModule>(out statModule);
    }

    public override void Init()
    {
        direction = Vector2.zero;
    }

    public void Move(Vector2 moveDirection)
    {
        direction = Vector2.ClampMagnitude(moveDirection, 1f);
    }

    public void Stop()
    {
        direction = Vector2.zero;

        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    public override void OnFixedUpdate()
    {
        if (body != null && statModule != null)
            body.linearVelocity = direction * statModule.GetStatValue(StatType.MoveSpeed);
    }

    public override void Release()
    {
        Stop();
    }
}