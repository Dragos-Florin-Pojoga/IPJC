using UnityEngine;

/// <summary>
/// Example implementation of a damageable enemy.
/// Implements IDamageable to receive damage from projectiles and contact damage.
/// </summary>
[RequireComponent(typeof(StatController))]
public class Enemy : MonoBehaviour, IDamageable
{
    // [Tooltip("Prefab for floating damage numbers")]
    // prefab is taken from the prefab directory
    private DamagePopup damagePopupPrefab;
    
    [Tooltip("Height offset for damage popup spawn")]
    public float popupHeight = 1.4f;

    private StatController m_stats;
    
    /// <summary>
    /// Event fired when this enemy takes damage. Used by AI controllers for aggro.
    /// </summary>
    public event System.Action OnDamageTaken;

    void Awake()
    {
        m_stats = GetComponent<StatController>();
        damagePopupPrefab = Resources.Load<DamagePopup>("Prefabs/Damage_Popup");
    }

    void Start()
    {
        // Subscribe in Start() to ensure StatController.Awake() has initialized m_resources
        FloatingHealthBar floatingHealthBar = transform.GetComponentInChildren<FloatingHealthBar>();
        if (floatingHealthBar != null) {
            Debug.Log($"FloatingHealthBar found on {gameObject.name}");
            m_stats.SubscribeToResource(StatType.Health, floatingHealthBar.UpdateHealthBar);
        }
    }
    
    /// <inheritdoc/>
    public StatController GetStatController() => m_stats;
    
    /// <inheritdoc/>
    public Transform GetTransform() => transform;

    /// <inheritdoc/>
    public void TakeHit(HitContext context)
    {
        FinalDamageResult result = DamageCalculator.CalculateHit(context);
        
        float healthBefore = m_stats.GetCurrentValue(StatType.Health);
        
        Debug.Log($"{gameObject.name} took {result.TotalDamage} damage ({(result.WasCritical ? "CRIT!" : "")}), {healthBefore} -> {healthBefore - result.TotalDamage} health remaining.");

        SpawnDamagePopup(result.TotalDamage, result.WasCritical);

        m_stats.ModifyResource(StatType.Health, -result.TotalDamage);
        
        // Notify AI of damage for aggro
        OnDamageTaken?.Invoke();

        foreach (var app in context.StatusEffects) {
            app.Effect.Apply(m_stats); 
        }

        if (m_stats.GetCurrentValue(StatType.Health) <= 0) {
            Destroy(gameObject);
        }
    }

    private void SpawnDamagePopup(float amount, bool crit)
    {
        if (damagePopupPrefab == null) return;

        Vector3 position = transform.position + Vector3.up * popupHeight;
        var popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);
        popup.SetDamage(amount, crit);
    }
}