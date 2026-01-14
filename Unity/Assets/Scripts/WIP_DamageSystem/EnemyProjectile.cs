using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A projectile fired by enemies. Uses the same SpellEffect pipeline as player projectiles
/// for full effect support (homing, damage conversion, status effects, etc.).
/// Uses raycast-based hit detection for reliable collision.
/// </summary>
public class EnemyProjectile : MonoBehaviour, IProjectile
{
    // --- Stats ---
    private float m_speed = 10.0f;
    private float m_lifetime = 5.0f;
    private float m_spawnTime;
    public Vector3 Direction { get; private set; }
    
    // --- Periodic Tick ---
    private float m_tickRate = float.MaxValue;
    private float m_nextTickTime;

    // --- Targeting ---
    [Header("Target Filter")]
    [Tooltip("Tags that can be damaged by this projectile")]
    public string[] damageableTags = { "Player" };
    
    [Header("Collision")]
    [Tooltip("Layers to check for collision")]
    public LayerMask collisionMask = ~0; // Everything by default

    // --- References ---
    private List<SpellEffect> m_runtimeEffects = new List<SpellEffect>();
    private StatController m_ownerStats;
    public StatController OwnerStats => m_ownerStats;
    public Transform Transform => transform;

    private bool m_isDestroyed = false;
    private Vector3 m_lastPosition;

    /// <summary>
    /// Initializes the projectile. Called by the EnemyAttackController or ShooterEnemyController.
    /// </summary>
    public void Initialize(List<SpellEffect> effects, Vector3 direction, StatController ownerStats)
    {
        m_runtimeEffects = effects;
        Direction = direction.normalized;
        m_ownerStats = ownerStats;
        m_spawnTime = Time.time;
        m_nextTickTime = Time.time + m_tickRate;
        m_lastPosition = transform.position;
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

        // Calculate new position
        Vector3 movement = Direction * m_speed * Time.deltaTime;
        Vector3 newPosition = transform.position + movement;
        
        // Raycast from last position to new position to detect hits
        float distance = movement.magnitude;
        if (distance > 0.001f)
        {
            if (Physics.Raycast(m_lastPosition, Direction, out RaycastHit hit, distance + 0.5f, collisionMask))
            {
                // Move to hit point
                transform.position = hit.point;
                
                // Handle the hit
                HandleHit(hit.collider.gameObject);
                return; // Don't continue moving if we hit something
            }
        }
        
        // Move to new position
        transform.position = newPosition;
        m_lastPosition = transform.position;

        // Check lifetime
        if (Time.time > m_spawnTime + m_lifetime) {
            DestroyProjectile(isLifetimeEnd: true);
            return;
        }

        // Update effects
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
        HandleHit(collision.gameObject);
    }
    
    void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }
    
    private void HandleHit(GameObject hitObject)
    {
        if (m_isDestroyed) return;

        // Check tag filter for damageables
        bool validTag = false;
        foreach (var tag in damageableTags) {
            if (hitObject.CompareTag(tag)) {
                validTag = true;
                break;
            }
        }
        
        if (validTag)
        {
            // Try to find IDamageable on hit object or parent
            IDamageable target = hitObject.GetComponent<IDamageable>();
            if (target == null) target = hitObject.GetComponentInParent<IDamageable>();
            
            if (target != null)
            {
                // Don't hit the owner
                if (m_ownerStats != null && target.GetTransform() == m_ownerStats.transform) return;

                HitContext context = new HitContext(target, m_ownerStats);

                // Run the SpellEffect pipeline to compile the hit
                foreach (var effect in m_runtimeEffects) {
                    effect.OnCompileHit(this, context);
                }

                target.TakeHit(context);
                Debug.Log($"Enemy projectile hit {hitObject.name}!");

                // Post-hit effects
                foreach (var effect in m_runtimeEffects) {
                    effect.OnHit(this, context);
                }
            }
        }

        // Destroy on impact with anything
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
