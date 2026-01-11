using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using static UnityEngine.UI.Image;

public class EnemyController : MonoBehaviour
{


    [Header("PatrolPoints")]
    [SerializeField] private Transform patrolParent;
    [SerializeField] private float waitTimeAtPoint = 2f;
    [SerializeField] private float arrivalDistance = 0.4f;


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
    [SerializeField] private float scanRotationSpeed = 120f; // grade/sec
    [SerializeField] private float scanDuration = 2.5f;

    private bool isScanning = false;
    private float scanTimer = 0f;




    [SerializeField] private float searchArrivalDistance = 0.8f;




    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField]
    private float maxHealth = 100f;

    private float currentHealth;
    private bool isDead = false;

    private Rigidbody rb;
    private Collider col;

    [SerializeField]
    private FloatingHealthBar healthBar;




    private enum AIState
    {
        Patrol,
        Aggro,
        Search
    }

    [SerializeField] private AIState currentState = AIState.Patrol;




    [Header("Aggro")]
    [SerializeField] private float loseAggroDelay = 0.5f;

    private float loseAggroTimer = 0f;



    private Vector3 lastSeenPlayerPosition;
    private bool hasLastSeenPosition = false;

    private bool reachedLastSeen = false;


    Animator animator;

    [SerializeField] Transform warriorVisual;

    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float attackCooldown = 1.5f;
    private float attackTimer = 0f;


    void Start()
    {

        animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        rb.isKinematic = true;

        if (healthBar != null)
            healthBar.UpdateHealthBar(currentHealth, maxHealth);

        patrolPoints = patrolParent.GetComponentsInChildren<Transform>()
            .Where(p => p != patrolParent)
            .ToArray();

        PickRandomPatrolPoint();
    }




    bool CheckVision()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 toPlayer = (player.position - origin).normalized;

        
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > visionAngle)
            return false;


        if (Physics.Raycast(origin, toPlayer, out RaycastHit hit, visionRange, visionMask))
        {
            Transform hitT = hit.collider.transform;
            if (hitT == player || hitT.IsChildOf(player))
                return true;
        }

        return false;
    }





    void PatrolBehaviour()
    {
        moveSpeed = 1f;
        if (patrolPoints.Length == 0) return;

        Transform target = patrolPoints[currentPatrolIndex];
        float distance = Vector3.Distance(transform.position, target.position);

        // ================= ARRIVED =================
        if (distance < 0.8f)
        {
            animator.SetBool("isWalking", false);

            rb.linearVelocity = Vector3.zero;

            
            if (!isScanning)
            {
                isWaiting = true;
                waitTimer += Time.fixedDeltaTime;

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

                // daca vede player-ul => STOP TOTAL
                if (CheckVision())
                {
                    Debug.Log("Player spotted during scan!");
                    rb.linearVelocity = Vector3.zero;
                    return;
                }


                // se roteste
                transform.Rotate(Vector3.up, scanRotationSpeed * Time.fixedDeltaTime);
                scanTimer += Time.fixedDeltaTime;


                // a terminat scanarea
                if (scanTimer >= scanDuration)
                {
                    isScanning = false;
                    PickRandomPatrolPoint();
                }

                return;
            }
        }

        // ================= MOVING =================
        if (!isWaiting && !isScanning)
        {
            MoveTowards(target.position, moveSpeed);
        }
    }



    void AggroBehaviour()
    {

        if (isDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // ===========================
        // 2. NORMAL ATTACK: MELEE
        // ===========================
        attackTimer += Time.fixedDeltaTime;

        if (distanceToPlayer <= attackRange)
        {
            rb.linearVelocity = Vector3.zero;

            animator.SetBool("isWalking", false);

            Vector3 dir = player.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    10f * Time.fixedDeltaTime
                );
            }

            if (attackTimer >= attackCooldown)
            {
                animator.SetTrigger("isAttacking");
                attackTimer = 0f;
            }

            return;
        }

        moveSpeed = 2.5f;
        MoveTowards(player.position, moveSpeed);



    }





    void SearchBehaviour()
    {
        if (!hasLastSeenPosition)
        {
            currentState = AIState.Patrol;
            return;
        }

        // ================= MOVE =================
        if (!reachedLastSeen)
        {
            float dist = Vector3.Distance(transform.position, lastSeenPlayerPosition);

            if (dist > searchArrivalDistance)
            {
                MoveTowards(lastSeenPlayerPosition, moveSpeed);
                return;
            }

            // ARRIVAL
            reachedLastSeen = true;
        }

        rb.linearVelocity = Vector3.zero;

        // ================= SCAN =================
        if (!isScanning)
        {
            isScanning = true;
            scanTimer = 0f;
        }

        animator.SetBool("isWalking", false);

        transform.Rotate(Vector3.up, scanRotationSpeed * Time.fixedDeltaTime);
        scanTimer += Time.fixedDeltaTime;

        if (CheckVision())
        {
            Debug.Log("Player found again!");
            reachedLastSeen = false;
            currentState = AIState.Aggro;
            return;
        }

        if (scanTimer >= scanDuration)
        {
            Debug.Log("Search failed -> PATROL");
            isScanning = false;
            reachedLastSeen = false;
            hasLastSeenPosition = false;
            currentState = AIState.Patrol;
            PickRandomPatrolPoint();
        }
    }





    void FixedUpdate()
    {
        if (isDead) return;

        switch (currentState)
        {
            case AIState.Patrol:
                PatrolBehaviour();
                break;

            case AIState.Aggro:
                AggroBehaviour();
                break;

            case AIState.Search:
                SearchBehaviour();
                break;
        }

    }



    void PickRandomPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, patrolPoints.Length);
        }
        while (newIndex == currentPatrolIndex && patrolPoints.Length > 1);

        currentPatrolIndex = newIndex;
    }


    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 flatTarget = new Vector3(
            target.x,
            transform.position.y,
            target.z
        );

        Vector3 direction = (flatTarget - transform.position).normalized;
        Vector3 newPos = rb.position + direction * speed * Time.fixedDeltaTime;

        rb.MovePosition(newPos);
        animator.SetBool("isWalking", true);

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }


    void OnDrawGizmos()
    {
        if (patrolParent == null) return;

        Gizmos.color = Color.cyan;
        foreach (Transform p in patrolParent)
            Gizmos.DrawSphere(p.position, 0.25f);

        Gizmos.color = canSeePlayer ? Color.red : Color.green;
        Gizmos.DrawLine(
            transform.position + Vector3.up * 0.5f,
            player.position
        );

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Gizmos.color = Color.yellow;

        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle, 0) * transform.forward;

        Gizmos.DrawLine(origin, origin + leftBoundary * visionRange);
        Gizmos.DrawLine(origin, origin + rightBoundary * visionRange);
    }



    public void Update()
    {
        if (isDead) return;

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            Debug.Log("Enemy takes damage!");
            TakeDamage(25f);
        }

        //bool seesPlayer = CheckVision();

        canSeePlayer = CheckVision();
        bool seesPlayer = canSeePlayer;

        switch (currentState)
        {
            case AIState.Patrol:
                if (seesPlayer)
                {
                    Debug.Log("ENTER AGGRO");
                    currentState = AIState.Aggro;
                    isWaiting = false;
                    isScanning = false;
                }
                break;

            case AIState.Aggro:
                if (seesPlayer)
                {


                    lastSeenPlayerPosition = player.position;
                    //because the player can be mid-air, the enemy can't reach the location => will be stuck there
                    lastSeenPlayerPosition.y = transform.position.y;


                    hasLastSeenPosition = true;
                    loseAggroTimer = 0f;

                }
                else
                {
                    loseAggroTimer += Time.deltaTime;
                    if (loseAggroTimer >= loseAggroDelay)
                    {
                        Debug.Log("Lost player -> SEARCH");
                        loseAggroTimer = 0f;
                        currentState = AIState.Search;
                    }
                }
                break;


            case AIState.Search:
                if (seesPlayer)
                {
                    Debug.Log("SEARCH -> AGGRO");
                    currentState = AIState.Aggro;

                    reachedLastSeen = false;
                    isScanning = false;
                    loseAggroTimer = 0f;

                    lastSeenPlayerPosition = player.position;
                    lastSeenPlayerPosition.y = transform.position.y;
                    hasLastSeenPosition = true;
                }
                break;
        }


    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null)
            healthBar.UpdateHealthBar(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }



    IEnumerator EnableCorpseCollider()
    {
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


        col.enabled = true;
        rb.isKinematic = true;

    }


    private void Die()
    {
        isDead = true;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.None;

        if (col != null)
            col.enabled = false;

        //animator.SetBool("isDead", true);
        animator.SetBool("isWalking", false);

        StartCoroutine(DestroyAfterDelay(5f));
        StartCoroutine(EnableCorpseCollider());
    }



    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}