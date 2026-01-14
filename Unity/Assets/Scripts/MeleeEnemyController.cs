using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Melee enemy AI controller. Extends BaseEnemyAI with close-range attack behavior.
/// Deals damage automatically when in range without requiring animation events.
/// </summary>
[RequireComponent(typeof(StatController))]
public class MeleeEnemyController : BaseEnemyAI
{
    [Header("Melee Attack")]
    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float attackCooldown = 1.5f;
    [Tooltip("Radius of the damage sphere when attacking")]
    [SerializeField] private float attackDamageRadius = 1.5f;
    [Tooltip("Offset from enemy center for attack sphere")]
    [SerializeField] private Vector3 attackOffset = new Vector3(0, 0.5f, 1f);
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [Tooltip("Delay after animation trigger before damage is dealt")]
    [SerializeField] private float damageDelay = 0.3f;
    
    [Header("Visual")]
    [SerializeField] private Transform warriorVisual;
    
    private float attackTimer = 0f;
    private Collider col;
    private StatController m_stats;
    
    protected override void Awake()
    {
        base.Awake();
        col = GetComponent<Collider>();
        m_stats = GetComponent<StatController>();
    }
    
    protected override void AggroBehavior()
    {
        if (isDead || player == null) return;
        
        agent.speed = aggroSpeed;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        attackTimer += Time.deltaTime;
        
        if (distanceToPlayer <= attackRange)
        {
            // In attack range - stop and attack
            agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);
            
            FacePlayer();
            
            if (attackTimer >= attackCooldown)
            {
                // Trigger attack animation
                if (animator != null) animator.SetTrigger("isAttacking");
                
                // Deal damage after a short delay
                StartCoroutine(DealDamageAfterDelay(damageDelay));
                
                attackTimer = 0f;
            }
        }
        else
        {
            // Chase the player
            agent.isStopped = false;
            agent.SetDestination(player.position);
            if (animator != null) animator.SetBool("isWalking", true);
        }
    }
    
    private IEnumerator DealDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (isDead) yield break;
        
        // Calculate attack sphere position in world space
        Vector3 attackPos = transform.position + transform.TransformDirection(attackOffset);
        
        // Find all colliders in the attack range
        Collider[] hits = Physics.OverlapSphere(attackPos, attackDamageRadius);
        
        foreach (var hit in hits)
        {
            // Check if it's the player (or any damageable with Player tag)
            if (hit.CompareTag("Player"))
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) damageable = hit.GetComponentInParent<IDamageable>();
                
                if (damageable != null && damageable.GetTransform() != transform)
                {
                    // Create hit context and deal damage
                    float contactDamage = m_stats.GetStatValue(StatType.ContactDamage);
                    HitContext context = new HitContext(damageable, m_stats);
                    context.Damages.Add(new DamageInstance { Type = damageType, Amount = contactDamage });
                    
                    damageable.TakeHit(context);
                    Debug.Log($"Melee hit player for {contactDamage} damage!");
                    break;
                }
            }
        }
    }
    
    public override void Die()
    {
        base.Die();
        
        StopAllCoroutines();
        
        if (col != null) col.enabled = false;
        if (animator != null) animator.SetBool("isWalking", false);
        
        StartCoroutine(DestroyAfterDelay(5f));
        StartCoroutine(EnableCorpseCollider());
    }
    
    private IEnumerator EnableCorpseCollider()
    {
        if (warriorVisual == null) yield break;
        
        Quaternion startRot = warriorVisual.rotation;
        Quaternion endRot = Quaternion.Euler(startRot.eulerAngles + new Vector3(90f, 0f, 0f));

        float duration = 1.2f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            warriorVisual.rotation = Quaternion.Slerp(startRot, endRot, t / duration);
            yield return null;
        }

        if (col != null) col.enabled = true;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw attack damage sphere
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
        Vector3 attackPos = transform.position + transform.TransformDirection(attackOffset);
        Gizmos.DrawWireSphere(attackPos, attackDamageRadius);
    }
}