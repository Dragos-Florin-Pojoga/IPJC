using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple destructible prop that implements IDamageable.
/// Attach to any GameObject with a StatController to make it destructible.
/// </summary>
/// <remarks>
/// Configure the StatController with Health (and optionally Armor, FireResistance) stats.
/// Set FireResistance to 100 to make the prop immune to fire damage.
/// </remarks>
[RequireComponent(typeof(StatController))]
public class DestructibleProp : MonoBehaviour, IDamageable
{
    [Header("Visual Feedback")]
    [Tooltip("Optional prefab for floating damage numbers")]
    public DamagePopup damagePopupPrefab;
    
    [Tooltip("Height offset for damage popup spawn")]
    public float popupHeight = 0.5f;
    
    [Header("Destruction")]
    [Tooltip("Optional particle effect to spawn on destruction")]
    public GameObject destructionEffect;
    
    [Tooltip("Time to wait before destroying the GameObject (0 = immediate)")]
    public float destructionDelay = 0f;
    
    [Header("Events")]
    [Tooltip("Called when the prop is destroyed")]
    public UnityEvent onDestroyed;
    
    [Tooltip("Called when the prop takes damage (passes damage amount)")]
    public UnityEvent<float> onDamaged;

    private StatController m_stats;
    private bool m_isDestroyed = false;

    void Awake()
    {
        m_stats = GetComponent<StatController>();
    }
    
    /// <inheritdoc/>
    public StatController GetStatController() => m_stats;
    
    /// <inheritdoc/>
    public Transform GetTransform() => transform;

    /// <inheritdoc/>
    public void TakeHit(HitContext context)
    {
        if (m_isDestroyed) return;
        
        FinalDamageResult result = DamageCalculator.CalculateHit(context);
        
        // Skip if no damage dealt
        if (result.TotalDamage <= 0) return;
        
        SpawnDamagePopup(result.TotalDamage, result.WasCritical);
        
        m_stats.ModifyResource(StatType.Health, -result.TotalDamage);
        onDamaged?.Invoke(result.TotalDamage);

        // Apply status effects
        foreach (var app in context.StatusEffects) {
            app.Effect.Apply(m_stats);
        }

        // Check for destruction
        if (m_stats.GetCurrentValue(StatType.Health) <= 0) {
            DestroyProp();
        }
    }
    
    private void SpawnDamagePopup(float amount, bool crit)
    {
        if (damagePopupPrefab == null) return;

        Vector3 position = transform.position + Vector3.up * popupHeight;
        var popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);
        popup.SetDamage(amount, crit);
    }
    
    private void DestroyProp()
    {
        if (m_isDestroyed) return;
        m_isDestroyed = true;
        
        if (destructionEffect != null) {
            Instantiate(destructionEffect, transform.position, transform.rotation);
        }
        
        onDestroyed?.Invoke();
        
        if (destructionDelay > 0) {
            Destroy(gameObject, destructionDelay);
        } else {
            Destroy(gameObject);
        }
    }
}
