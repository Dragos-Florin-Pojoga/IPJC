/////////////////////////////////
// WIP / VERY EXPERIMENTAL !!! //
/////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A projectile fired by enemies. Uses the same SpellEffect pipeline as player projectiles
/// for full effect support (homing, damage conversion, status effects, etc.).
/// </summary>
public class EnemyProjectile : MonoBehaviour, IProjectile
{
    // --- Stats ---
    private float m_speed = 0.0f;
    private float m_lifetime = 0.0f;
    private float m_spawnTime;
    public Vector3 Direction { get; private set; }
    
    // --- Periodic Tick ---
    private float m_tickRate = float.MaxValue;
    private float m_nextTickTime;

    // --- Targeting ---
    [Header("Target Filter")]
    [Tooltip("Tags that can be damaged by this projectile")]
    public string[] damageableTags = { "Player" };

    // --- References ---
    private List<SpellEffect> m_runtimeEffects = new List<SpellEffect>();
    private StatController m_ownerStats;
    public StatController OwnerStats => m_ownerStats;
    public Transform Transform => transform;

    private bool m_isDestroyed = false;

    /// <summary>
    /// Initializes the projectile. Called by the EnemyAttackController.
    /// </summary>
    public void Initialize(List<SpellEffect> effects, Vector3 direction, StatController ownerStats)
    {
        m_runtimeEffects = effects;
        Direction = direction.normalized;
        m_ownerStats = ownerStats;
        m_spawnTime = Time.time;
        m_nextTickTime = Time.time + m_tickRate;
        transform.rotation = Quaternion.LookRotation(Direction);

        foreach (var effect in m_runtimeEffects) {
            effect.Initialize(this as IProjectile);
        }
    }

    /// <summary>
    /// Called by effects to set projectile stats.
    /// </summary>
    public void SetStats(float speed, float lifetime, float size, float tickRate)
    {
        m_speed = speed;
        m_lifetime = lifetime;
        transform.localScale *= size;
        m_tickRate = tickRate;
    }
    
    /// <summary>
    /// Called by effects that modify flight path.
    /// </summary>
    public void SetDirection(Vector3 newDirection)
    {
        Direction = newDirection.normalized;
        transform.rotation = Quaternion.LookRotation(Direction);
    }

    void Update()
    {
        if (m_isDestroyed) return;

        transform.position += Direction * m_speed * Time.deltaTime;

        if (Time.time > m_spawnTime + m_lifetime) {
            DestroyProjectile(isLifetimeEnd: true);
            return;
        }

        foreach (var effect in m_runtimeEffects) {
            effect.OnUpdate(this);
        }

        if (Time.time > m_nextTickTime) {
            foreach (var effect in m_runtimeEffects) {
                effect.OnTick(this);
            }
            m_nextTickTime += m_tickRate;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (m_isDestroyed) return;

        // Check tag filter
        bool validTag = false;
        foreach (var tag in damageableTags) {
            if (collision.gameObject.CompareTag(tag)) {
                validTag = true;
                break;
            }
        }
        
        if (validTag && collision.gameObject.TryGetComponent<IDamageable>(out var target)) {
            // Don't hit the owner
            if (m_ownerStats != null && target.GetTransform() == m_ownerStats.transform) return;

            HitContext context = new HitContext(target, m_ownerStats);

            // Run the SpellEffect pipeline to compile the hit
            foreach (var effect in m_runtimeEffects) {
                effect.OnCompileHit(this, context);
            }

            target.TakeHit(context);

            // Post-hit effects
            foreach (var effect in m_runtimeEffects) {
                effect.OnHit(this, context);
            }
        }

        // Destroy on impact
        // TODO: A "Piercing" effect would set a flag to prevent this
        DestroyProjectile(isLifetimeEnd: false);
    }

    private void DestroyProjectile(bool isLifetimeEnd)
    {
        if (m_isDestroyed) return;
        m_isDestroyed = true;
        
        if (isLifetimeEnd) {
            foreach (var effect in m_runtimeEffects) {
                effect.OnLifetimeEnd(this);
            }
        }
        
        Destroy(gameObject);
    }
}
