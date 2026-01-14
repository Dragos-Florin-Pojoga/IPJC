using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Deals contact damage when colliding with IDamageable targets.
/// Damage scales with the ContactDamage stat and respects per-target cooldowns.
/// </summary>
/// <remarks>
/// Supports both trigger and non-trigger colliders.
/// For CharacterController players, use the public DealDamageTo() method from OnControllerColliderHit.
/// </remarks>
[RequireComponent(typeof(StatController))]
public class ContactDamageDealer : MonoBehaviour
{
    [Header("Damage Configuration")]
    [Tooltip("Damage type dealt on contact")]
    public DamageType damageType = DamageType.Physical;
    
    [Tooltip("Base damage multiplier (multiplied by ContactDamage stat)")]
    public float damageMultiplier = 1f;
    
    [Tooltip("Tags that can be damaged (e.g., 'Player')")]
    public string[] damageableTags = { "Player" };
    
    [Header("Status Effects (Optional)")]
    public List<StatusEffectApplication> statusEffects = new List<StatusEffectApplication>();
    
    private StatController m_stats;
    private Dictionary<IDamageable, float> m_hitCooldowns = new Dictionary<IDamageable, float>();
    
    void Awake()
    {
        m_stats = GetComponent<StatController>();
    }
    
    void Update()
    {
        var expiredTargets = new List<IDamageable>();
        
        foreach (var kvp in m_hitCooldowns) {
            if (Time.time >= kvp.Value) {
                expiredTargets.Add(kvp.Key);
            }
        }
        
        foreach (var target in expiredTargets) {
            m_hitCooldowns.Remove(target);
        }
    }
    
    void OnCollisionEnter(Collision collision) => TryDamage(collision.gameObject);
    void OnCollisionStay(Collision collision) => TryDamage(collision.gameObject);
    void OnTriggerEnter(Collider other) => TryDamage(other.gameObject);
    void OnTriggerStay(Collider other) => TryDamage(other.gameObject);
    
    private void TryDamage(GameObject target)
    {
        bool validTag = false;
        foreach (var tag in damageableTags) {
            if (target.CompareTag(tag)) {
                validTag = true;
                break;
            }
        }
        if (!validTag) return;
        
        if (!target.TryGetComponent<IDamageable>(out var damageable)) return;
        
        DealDamageTo(damageable);
    }

    /// <summary>
    /// Deals contact damage to the specified target.
    /// Can be called externally from CharacterController.OnControllerColliderHit.
    /// </summary>
    public void DealDamageTo(IDamageable damageable)
    {
        if (damageable == null) return;
        if (m_hitCooldowns.ContainsKey(damageable)) return;
        
        float contactDamage = m_stats.GetStatValue(StatType.ContactDamage) * damageMultiplier;
        
        HitContext context = new HitContext(damageable, m_stats);
        context.Damages.Add(new DamageInstance { Type = damageType, Amount = contactDamage });
        
        foreach (var effect in statusEffects) {
            context.StatusEffects.Add(effect);
        }
        
        damageable.TakeHit(context);
        
        float hitCooldown = m_stats.GetStatValue(StatType.ContactCooldown);
        if (hitCooldown <= 0f) hitCooldown = 0.5f;
        m_hitCooldowns[damageable] = Time.time + hitCooldown;
    }
}
