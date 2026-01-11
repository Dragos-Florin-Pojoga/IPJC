using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using static UnityEngine.UI.Image;

public class ShooterEnemyController: MonoBehaviour
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


    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootCooldown = 1.5f;
    [SerializeField] private float shootRange = 12f;
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float accuracyDegrees = 7f; // +/- grade pentru imprecizie

    private float shootTimer = 0f;


    Animator animator;


    void Start()
    {

        animator = GetComponent<Animator>();

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


    void ShootAtPlayer()
    {

        Debug.Log("Trage!");

        animator.SetTrigger("isAttacking");

        if (projectilePrefab == null || shootPoint == null || player == null) return;

        Vector3 direction = (player.position + Vector3.up * 1.2f - shootPoint.position).normalized;
        direction = ApplyAccuracy(direction, accuracyDegrees);

        GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.LookRotation(direction));
        Rigidbody rbProj = proj.GetComponent<Rigidbody>();
        if (rbProj != null)
            rbProj.linearVelocity = direction * projectileSpeed;
    }



    Vector3 ApplyAccuracy(Vector3 dir, float maxDegrees)
    {
        float yaw = Random.Range(-maxDegrees, maxDegrees);
        float pitch = Random.Range(-maxDegrees, maxDegrees);
        Quaternion deviation = Quaternion.Euler(pitch, yaw, 0);
        return deviation * dir;
    }



    void AggroBehaviour()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // Daca e in range, sta pe loc si trage
        if (distance <= shootRange)
        {
            rb.linearVelocity = Vector3.zero;
            animator.SetBool("isWalking", false);

            // Se uita spre jucator
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    8f * Time.fixedDeltaTime
                );
            }

            // Tragere cu cooldown si acuratete
            shootTimer += Time.fixedDeltaTime;
            if (shootTimer >= shootCooldown)
            {
                ShootAtPlayer();
                shootTimer = 0f;
            }
        }
        else
        {
            // Daca nu e in range, se apropie de jucator
            MoveTowards(player.position, moveSpeed);
        }
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

    private void Die()
    {

        animator.SetBool("isDead", true);

        isDead = true;


        StartCoroutine(DestroyAfterDelay(5f));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}