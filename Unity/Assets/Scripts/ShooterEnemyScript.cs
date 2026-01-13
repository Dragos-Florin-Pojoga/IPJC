using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NavMeshAgent))]
public class ShooterEnemyController : MonoBehaviour
{
    [Header("NavMesh")]
    private NavMeshAgent agent;

    [Header("PatrolPoints")]
    [SerializeField] private Transform patrolParent;
    [SerializeField] private float waitTimeAtPoint = 2f;

    private bool isWaiting = false;
    private Transform[] patrolPoints;
    private int currentPatrolIndex = -1;
    private float waitTimer = 0f;

    [Header("Vision")]
    [SerializeField] private float visionRange = 15f;
    [SerializeField] private float visionAngle = 60f;
    [SerializeField] private LayerMask visionMask;
    [SerializeField] private Transform player;
    private bool canSeePlayer = false;

    [Header("Scan")]
    [SerializeField] private float scanRotationSpeed = 120f;
    [SerializeField] private float scanDuration = 2.5f;

    private bool isScanning = false;
    private float scanTimer = 0f;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    private Collider col;
    [SerializeField] private FloatingHealthBar healthBar;

    private enum AIState { Patrol, Aggro, Search }
    [SerializeField] private AIState currentState = AIState.Patrol;

    [Header("Aggro")]
    [SerializeField] private float loseAggroDelay = 0.5f;
    private float loseAggroTimer = 0f;
    private Vector3 lastSeenPlayerPosition;
    private bool hasLastSeenPosition = false;
    private bool reachedLastSeen = false;

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootCooldown = 1.5f;
    [SerializeField] private float shootRange = 12f;
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float accuracyDegrees = 7f;

    private float shootTimer = 0f;

    Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider>();

        currentHealth = maxHealth;
        if (healthBar != null)
            healthBar.UpdateHealthBar(currentHealth, maxHealth);

        patrolPoints = patrolParent.GetComponentsInChildren<Transform>()
            .Where(p => p != patrolParent)
            .ToArray();

        agent.updateRotation = true;
        agent.updatePosition = true;

        PickRandomPatrolPoint();
    }

    bool HasReachedDestination()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    return true;
            }
        }
        return false;
    }

    bool CheckVision()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 targetPos = player.position + Vector3.up * 1f;

        Vector3 toPlayer = (targetPos - origin).normalized;

        if (Vector3.Angle(transform.forward, toPlayer) > visionAngle)
            return false;

        if (Physics.Raycast(origin, toPlayer, out RaycastHit hit, visionRange, visionMask))
        {
            if (hit.collider.transform == player || hit.collider.transform.IsChildOf(player))
                return true;
        }
        return false;
    }

    void PatrolBehaviour()
    {
        agent.speed = 1.5f;

        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);

        if (HasReachedDestination())
        {
            animator.SetBool("isWalking", false);
            agent.isStopped = true;

            if (!isScanning)
            {
                isWaiting = true;
                waitTimer += Time.deltaTime;

                if (waitTimer >= waitTimeAtPoint)
                {
                    waitTimer = 0f;
                    isWaiting = false;
                    isScanning = true;
                    scanTimer = 0f;
                }
                return;
            }

            if (isScanning)
            {
                transform.Rotate(Vector3.up, scanRotationSpeed * Time.deltaTime);
                scanTimer += Time.deltaTime;

                if (CheckVision())
                    return;

                if (scanTimer >= scanDuration)
                {
                    isScanning = false;
                    agent.isStopped = false;
                    PickRandomPatrolPoint();
                }
                return;
            }
        }
        else
        {
            animator.SetBool("isWalking", true);
            agent.isStopped = false;
        }
    }

    void AggroBehaviour()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= shootRange)
        {
            agent.isStopped = true;
            animator.SetBool("isWalking", false);

            Vector3 lookDir = (player.position - transform.position);
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 8f * Time.deltaTime);

            shootTimer += Time.deltaTime;
            if (shootTimer >= shootCooldown)
            {
                ShootAtPlayer();
                shootTimer = 0f;
            }
        }
        else
        {
            agent.isStopped = false;
            agent.speed = 2.5f;
            agent.SetDestination(player.position);
            animator.SetBool("isWalking", true);
        }
    }

    void SearchBehaviour()
    {
        if (!hasLastSeenPosition)
        {
            currentState = AIState.Patrol;
            return;
        }

        agent.speed = 2f;

        if (!reachedLastSeen)
        {
            agent.SetDestination(lastSeenPlayerPosition);
            animator.SetBool("isWalking", true);

            if (HasReachedDestination())
            {
                reachedLastSeen = true;
                agent.isStopped = true;
                animator.SetBool("isWalking", false);
            }
        }
        else
        {
            if (!isScanning)
            {
                isScanning = true;
                scanTimer = 0f;
            }

            transform.Rotate(Vector3.up, scanRotationSpeed * Time.deltaTime);
            scanTimer += Time.deltaTime;

            if (CheckVision())
            {
                reachedLastSeen = false;
                currentState = AIState.Aggro;
                return;
            }

            if (scanTimer >= scanDuration)
            {
                isScanning = false;
                reachedLastSeen = false;
                hasLastSeenPosition = false;
                currentState = AIState.Patrol;
                PickRandomPatrolPoint();
            }
        }
    }

    void ShootAtPlayer()
    {
        animator.SetTrigger("isAttacking");

        if (projectilePrefab == null || shootPoint == null || player == null) return;

        Vector3 direction = (player.position + Vector3.up * 1.2f - shootPoint.position).normalized;
        direction = ApplyAccuracy(direction, accuracyDegrees);

        GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.LookRotation(direction));
        Rigidbody rbProj = proj.GetComponent<Rigidbody>();
        if (rbProj != null)
            rbProj.linearVelocity = direction * projectileSpeed;
    }

    Vector3 ApplyAccuracy(Vector3 dir, float max)
    {
        float yaw = Random.Range(-max, max);
        float pitch = Random.Range(-max, max);
        return Quaternion.Euler(pitch, yaw, 0) * dir;
    }

    void PickRandomPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        int newIndex;
        do newIndex = Random.Range(0, patrolPoints.Length);
        while (newIndex == currentPatrolIndex && patrolPoints.Length > 1);
        currentPatrolIndex = newIndex;
    }

    public void Update()
    {
        if (isDead) return;

        if (Keyboard.current.hKey.wasPressedThisFrame)
            TakeDamage(25f);

        canSeePlayer = CheckVision();
        bool seesPlayer = canSeePlayer;

        switch (currentState)
        {
            case AIState.Patrol:
                if (seesPlayer)
                {
                    currentState = AIState.Aggro;
                    isWaiting = false;
                    isScanning = false;
                }
                else PatrolBehaviour();
                break;

            case AIState.Aggro:
                if (seesPlayer)
                {
                    lastSeenPlayerPosition = player.position;
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(lastSeenPlayerPosition, out hit, 2f, NavMesh.AllAreas))
                        lastSeenPlayerPosition = hit.position;

                    hasLastSeenPosition = true;
                    loseAggroTimer = 0f;
                    AggroBehaviour();
                }
                else
                {
                    loseAggroTimer += Time.deltaTime;
                    if (loseAggroTimer >= loseAggroDelay)
                    {
                        loseAggroTimer = 0f;
                        currentState = AIState.Search;
                    }
                }
                break;

            case AIState.Search:
                if (seesPlayer)
                {
                    currentState = AIState.Aggro;
                    reachedLastSeen = false;
                    isScanning = false;
                    loseAggroTimer = 0f;

                    lastSeenPlayerPosition = player.position;
                    hasLastSeenPosition = true;
                }
                else SearchBehaviour();
                break;
        }
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (healthBar != null) healthBar.UpdateHealthBar(currentHealth, maxHealth);

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        if (agent != null) agent.enabled = false;
        if (col != null) col.enabled = false;
        animator.SetBool("isDead", true);
        StartCoroutine(DestroyAfterDelay(5f));
    }

    IEnumerator DestroyAfterDelay(float t)
    {
        yield return new WaitForSeconds(t);
        Destroy(gameObject);
    }
}
