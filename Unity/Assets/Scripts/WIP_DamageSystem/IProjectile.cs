using UnityEngine;

/// <summary>
/// Interface for projectiles that can work with SpellEffects.
/// Implemented by both Projectile (player) and EnemyProjectile.
/// </summary>
public interface IProjectile
{
    /// <summary>The owner's StatController (for damage calculation)</summary>
    StatController OwnerStats { get; }
    
    /// <summary>The current flight direction (normalized)</summary>
    Vector3 Direction { get; }
    
    /// <summary>The projectile's Transform</summary>
    Transform Transform { get; }
    
    /// <summary>
    /// Sets basic projectile stats. Called by Effect_BaseProjectileStats.
    /// </summary>
    void SetStats(float speed, float lifetime, float size, float tickRate);
    
    /// <summary>
    /// Changes the flight direction. Called by homing/tracking effects.
    /// </summary>
    void SetDirection(Vector3 newDirection);
}
