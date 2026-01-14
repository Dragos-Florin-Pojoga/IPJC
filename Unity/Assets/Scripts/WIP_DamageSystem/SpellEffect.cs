using UnityEngine;

/// <summary>
/// Base class for all spell effects. Effects are ScriptableObjects that modify
/// projectile behavior and damage through a pipeline of callbacks.
/// </summary>
/// <remarks>
/// Effects are applied in the order they appear in the SpellDefinition.
/// Each callback serves a specific purpose in the projectile lifecycle.
/// </remarks>
public abstract class SpellEffect : ScriptableObject
{
    /// <summary>
    /// Called once when the projectile is spawned.
    /// Use for setting stats (speed, lifetime, size, tick rate).
    /// </summary>
    public virtual void Initialize(IProjectile projectile) { }

    /// <summary>
    /// Called every frame while the projectile is alive.
    /// Use for homing, wobbling, or other continuous behaviors.
    /// </summary>
    public virtual void OnUpdate(IProjectile projectile) { }

    /// <summary>
    /// Called on a fixed timer (configurable tick rate).
    /// Use for periodic effects like DoT application areas.
    /// </summary>
    public virtual void OnTick(IProjectile projectile) { }

    /// <summary>
    /// Called BEFORE the target's TakeHit. This is the damage "pipeline" phase.
    /// Use to modify the HitContext: add damage, convert damage types, add status effects.
    /// </summary>
    public virtual void OnCompileHit(IProjectile projectile, HitContext context) { }

    /// <summary>
    /// Called AFTER the target's TakeHit.
    /// Use for "on hit" effects like explosions, chains, or additional hits.
    /// </summary>
    public virtual void OnHit(IProjectile projectile, HitContext context) { }

    /// <summary>
    /// Called just before the projectile is destroyed by lifetime ending.
    /// Use for "on expire" effects like explosions at destination.
    /// </summary>
    public virtual void OnLifetimeEnd(IProjectile projectile) { }
}