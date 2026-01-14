using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

/// <summary>
/// Jumper/Dash enemy AI controller. Extends BaseEnemyAI with leap attack behavior.
/// Leaps at player and deals damage on landing.
/// </summary>
[RequireComponent(typeof(StatController))]
public class JumperEnemyController : BaseEnemyAI
{
    [Header("Leap Attack")]
    [Tooltip("Maximum distance to start a leap")]
    [SerializeField] private float leapRange = 8f;
    [Tooltip("Minimum distance to start a leap (won't leap if too close)")]
    [SerializeField] private float minLeapRange = 3f;
    [Tooltip("Cooldown between leaps")]
    [SerializeField] private float leapCooldown = 2.5f;
    [Tooltip("Time in the air during leap")]
    [SerializeField] private float leapDuration = 0.5f;
    [Tooltip("Height of the leap arc")]
    [SerializeField] private float leapHeight = 2f;
    [Tooltip("Normal chase speed")]
    [SerializeField] private float baseChaseSpeed = 2.5f;
    
    [Header("Damage")]
    [Tooltip("Radius of the damage check on landing")]
    [SerializeField] private float landingDamageRadius = 2f;
    [SerializeField] private DamageType damageType = DamageType.Physical;
    
    [Header("Melee Attack")]
    [Tooltip("Range for close-range melee attack")]
    [SerializeField] private float attackRange = 2.0f;
    [Tooltip("Cooldown for melee attack")]
    [SerializeField] private float attackCooldown = 1.5f;
    [Tooltip("Delay before melee damage is dealt")]
    [SerializeField] private float meleeDamageDelay = 0.3f;
    [SerializeField] private float meleeDamageRadius = 1.5f;
    [SerializeField] private Vector3 meleeOffset = new Vector3(0, 0.5f, 1f);
    
    [Header("Visual")]
    [SerializeField] private Transform spiderVisual;
    
    private bool isLeaping = false;
    private float leapCooldownTimer = 0f;
    private float attackTimer = 0f;
    
    private Rigidbody rb;
    private Collider col;
    private StatController m_stats;
    
    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        m_stats = GetComponent<StatController>();
        
        if (rb != null) {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
    
    protected override void AggroBehavior()
    {
        if (isDead || player == null) return;
        
        // Don't do anything while leaping
        if (isLeaping) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        attackTimer += Time.deltaTime;
        leapCooldownTimer += Time.deltaTime;
        
        // Close range melee attack
        if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);
            
            FacePlayer();
            
            if (attackTimer >= attackCooldown)
            {
                if (animator != null) animator.SetTrigger("isAttacking");
                StartCoroutine(DealMeleeDamageAfterDelay(meleeDamageDelay));
                attackTimer = 0f;
            }
            return;
        }
        
        // In leap range - start leap
        if (distanceToPlayer > minLeapRange && distanceToPlayer <= leapRange && leapCooldownTimer >= leapCooldown)
        {
            StartCoroutine(PerformLeap());
            return;
        }
        
        // Chase the player
        agent.isStopped = false;
        agent.speed = baseChaseSpeed;
        agent.SetDestination(player.position);
        if (animator != null) animator.SetBool("isWalking", true);
    }
    
    private IEnumerator PerformLeap()
    {
        isLeaping = true;
        leapCooldownTimer = 0f;
        
        // Store positions
        Vector3 startPos = transform.position;
        Vector3 targetPos = player.position;
        
        // Snap target to navmesh
        if (NavMesh.SamplePosition(targetPos, out var navHit, 2f, NavMesh.AllAreas)) {
            targetPos = navHit.position;
        }
        
        // Stop agent during leap
        agent.isStopped = true;
        agent.updatePosition = false;
        
        // Trigger animation
        if (animator != null) animator.SetTrigger("isAttacking");
        
        // Face target
        Vector3 lookDir = (targetPos - startPos);
        lookDir.y = 0;
        if (lookDir != Vector3.zero) {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
        
        float elapsed = 0f;
        
        while (elapsed < leapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / leapDuration;
            
            // Horizontal movement
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            
            // Add arc (parabola)
            float arc = 4f * leapHeight * t * (1f - t);
            currentPos.y = Mathf.Lerp(startPos.y, targetPos.y, t) + arc;
            
            transform.position = currentPos;
            
            yield return null;
        }
        
        // Ensure we land at the target
        transform.position = targetPos;
        
        // Re-enable agent
        agent.Warp(targetPos);
        agent.updatePosition = true;
        agent.isStopped = false;
        
        // Deal damage on landing
        DealLandingDamage();
        
        isLeaping = false;
    }
    
    private void DealLandingDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, landingDamageRadius);
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) damageable = hit.GetComponentInParent<IDamageable>();
                
                if (damageable != null && damageable.GetTransform() != transform)
                {
                    float contactDamage = m_stats.GetStatValue(StatType.ContactDamage);
                    HitContext context = new HitContext(damageable, m_stats);
                    context.Damages.Add(new DamageInstance { Type = damageType, Amount = contactDamage });
                    
                    damageable.TakeHit(context);
                    Debug.Log($"Jumper landing hit player for {contactDamage} damage!");
                    break;
                }
            }
        }
    }
    
    private IEnumerator DealMeleeDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (isDead || isLeaping) yield break;
        
        Vector3 attackPos = transform.position + transform.TransformDirection(meleeOffset);
        Collider[] hits = Physics.OverlapSphere(attackPos, meleeDamageRadius);
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) damageable = hit.GetComponentInParent<IDamageable>();
                
                if (damageable != null && damageable.GetTransform() != transform)
                {
                    float contactDamage = m_stats.GetStatValue(StatType.ContactDamage);
                    HitContext context = new HitContext(damageable, m_stats);
                    context.Damages.Add(new DamageInstance { Type = damageType, Amount = contactDamage });
                    
                    damageable.TakeHit(context);
                    Debug.Log($"Jumper melee hit player for {contactDamage} damage!");
                    break;
                }
            }
        }
    }
    
    public override void Die()
    {
        base.Die();
        
        StopAllCoroutines();
        isLeaping = false;
        
        if (rb != null) {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        if (col != null) col.enabled = false;
        if (animator != null) animator.SetBool("isDead", true);
        
        StartCoroutine(DestroyAfterDelay(5f));
        StartCoroutine(EnableCorpseCollider());
    }
    
    private IEnumerator EnableCorpseCollider()
    {
        if (spiderVisual == null) yield break;
        
        Quaternion startRot = spiderVisual.rotation;
        Quaternion endRot = Quaternion.Euler(startRot.eulerAngles + new Vector3(180f, 0f, 0f));

        float duration = 1.2f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            spiderVisual.rotation = Quaternion.Slerp(startRot, endRot, t / duration);
            yield return null;
        }

        if (col != null) col.enabled = true;
        if (rb != null) rb.isKinematic = true;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        
        // Draw melee attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw leap range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, leapRange);
        
        // Draw min leap range
        Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, minLeapRange);
        
        // Draw landing damage radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, landingDamageRadius);
    }
}
