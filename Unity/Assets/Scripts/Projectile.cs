/////////////////////////////////
// WIP / VERY EXPERIMENTAL !!! //
/////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    // --- Stats ---
    private float m_speed = 0.0f;
    private float m_lifetime = 0.0f;
    private float m_spawnTime;
    public Vector3 Direction { get; private set; }
    
    // --- Periodic Tick ---
    private float m_tickRate = float.MaxValue;
    private float m_nextTickTime;

    // --- References ---
    private List<SpellEffect> m_runtimeEffects = new List<SpellEffect>();
    private StatController m_ownerStats;
    public StatController OwnerStats => m_ownerStats;

    private bool m_isDestroyed = false;

    // Initializes the projectile. Called by the Weapon.
    public void Initialize(List<SpellEffect> effects, Vector3 direction, StatController ownerStats)
    {
        m_runtimeEffects = effects;
        Direction = direction.normalized;
        m_ownerStats = ownerStats;
        m_spawnTime = Time.time;
        m_nextTickTime = Time.time + m_tickRate;
        transform.rotation = Quaternion.LookRotation(Direction);

        foreach (var effect in m_runtimeEffects) {
            effect.Initialize(this);
        }
    }

    // Called by effects
    public void SetStats(float speed, float lifetime, float size, float tickRate)
    {
        m_speed = speed;
        m_lifetime = lifetime;
        transform.localScale *= size;
        m_tickRate = tickRate;
    }
    
    // Called by effects that modify flight path
    public void SetDirection(Vector3 newDirection)
    {
        Direction = newDirection.normalized;
        transform.rotation = Quaternion.LookRotation(Direction);
    }

    void Update()
    {
        Debug.DrawLine(transform.position, transform.position + transform.forward * 2, Color.red, 0.1f, false);
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

        if (collision.gameObject.TryGetComponent<IDamageable>(out var target)) {
            // Don't hit the owner
            if (target.GetTransform() == m_ownerStats.transform) return;

            HitContext context = new HitContext(target, m_ownerStats);

            foreach (var effect in m_runtimeEffects) {
                effect.OnCompileHit(this, context);
            }

            target.TakeHit(context);

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