using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;

/// <summary>
/// A class that toggles colliders depending on actor state
/// </summary>
public class ActorCollisionToggler : ActorModule
{
    [SerializeField] private List<Collider> Colliders;

    public override void OnActorStateChanged(ActorState oldState, ActorState newState)
    {
        base.OnActorStateChanged(oldState, newState);
        if(Colliders == null || Colliders.Count == 0) return;
        foreach (var col in Colliders)
        {
            bool toggle = newState == ActorState.Spawned;
            col.enabled = toggle;
        }
    }
}