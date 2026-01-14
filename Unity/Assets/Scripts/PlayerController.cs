using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(StatController))]
public class PlayerControllerClean : MonoBehaviour, IDamageable
{
    [Header("Movement Settings")]
    [SerializeField] private float m_walkSpeed = 5f;
    [SerializeField] private float m_sprintSpeed = 8f;
    [SerializeField] private float m_jumpHeight = 1.8f;
    [SerializeField] private float m_gravity = -45f;

    [Header("Double Jump")]
    [SerializeField] private bool m_canDoubleJump = true;
    [SerializeField] private float m_doubleJumpHeight = 1.5f;

    [Header("Look Settings")]
    [SerializeField] private float m_mouseSensitivity = 30f;

    [Header("Camera Setup")]
    // The FPV Camera should be a child object of the PlayerController object
    [SerializeField] private Transform m_fpvCameraTransform;

    [Header("Damage Settings")]
    [SerializeField] private float m_invincibilityDuration = 0.5f;

    private CharacterController m_characterController;
    private PlayerControls m_controls;
    private StatController m_stats;

    private Vector3 m_velocity;
    private float m_xRotation = 0f;
    private int m_jumpCount = 0;
    private float m_invincibilityTimer = 0f;

    // IDamageable implementation
    public StatController GetStatController() => m_stats;
    public Transform GetTransform() => transform;

    public void TakeHit(HitContext context)
    {
        // Check invincibility frames
        if (m_invincibilityTimer > 0f) return;

        FinalDamageResult result = DamageCalculator.CalculateHit(context);

        float healthBefore = m_stats.GetCurrentValue(StatType.Health);
        Debug.Log($"Player took {result.TotalDamage} damage{(result.WasCritical ? " (CRIT!)" : "")}, health: {healthBefore} -> {healthBefore - result.TotalDamage}");

        m_stats.ModifyResource(StatType.Health, -result.TotalDamage);

        // Flash damage vignette
        if (DamageVignette.Instance != null) {
            DamageVignette.Instance.Flash();
        }

        // Apply status effects
        foreach (var app in context.StatusEffects) {
            app.Effect.Apply(m_stats);
        }

        // Start invincibility frames
        m_invincibilityTimer = m_invincibilityDuration;

        // Check death
        if (m_stats.GetCurrentValue(StatType.Health) <= 0f) {
            OnPlayerDeath();
        }
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Player has died!");
        // TODO: Implement death handling (respawn, game over screen, etc.)
        // For now, just reload the current scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void Awake()
    {
        m_characterController = GetComponent<CharacterController>();
        m_stats = GetComponent<StatController>();

        m_controls = new PlayerControls();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (m_fpvCameraTransform != null) {
            m_fpvCameraTransform.gameObject.SetActive(true);
        }
    }

    void Start()
    {
        // Find the global player health bar canvas and subscribe to health changes
        GameObject healthBarCanvas = GameObject.Find("PlayerHealthBarCanvas");
        if (healthBarCanvas != null) {
            FloatingHealthBar floatingHealthBar = healthBarCanvas.GetComponentInChildren<FloatingHealthBar>();
            if (floatingHealthBar != null) {
                Debug.Log("PlayerHealthBarCanvas found and subscribed to player health.");
                m_stats.SubscribeToResource(StatType.Health, floatingHealthBar.UpdateHealthBar);
            } else {
                Debug.LogWarning("PlayerHealthBarCanvas found but no FloatingHealthBar component in children.");
            }
        } else {
            Debug.LogWarning("PlayerHealthBarCanvas not found in scene.");
        }
    }

    void OnEnable()
    {
        m_controls.Player.Enable();
    }

    void OnDisable()
    {
        m_controls.Player.Disable();
    }

    private void Update()
    {
        // Tick invincibility
        if (m_invincibilityTimer > 0f) {
            m_invincibilityTimer -= Time.deltaTime;
        }

        HandleGroundCheck();
        HandleJump();
        HandleLook();
        HandleMovement();
        HandleGravity();
    }


    private void HandleJump()
    {
        bool jumpInputHeld = m_controls.Player.Jump.IsPressed();
        bool jumpInputPressedThisFrame = m_controls.Player.Jump.WasPerformedThisFrame();

        if (m_characterController.isGrounded) { // Grounded Jump
            if (jumpInputHeld && m_velocity.y < 0) {
                m_jumpCount = 1;
                m_velocity.y = Mathf.Sqrt(m_jumpHeight * -2f * m_gravity);
            }
        } else { // Double Jump
            if (m_canDoubleJump && m_jumpCount < 2 && jumpInputPressedThisFrame) {
                m_jumpCount = 2;
                m_velocity.y = Mathf.Sqrt(m_doubleJumpHeight * -2f * m_gravity);
            }
        }
    }

    private void HandleGroundCheck()
    {
        bool isGrounded = m_characterController.isGrounded;

        if (isGrounded && m_velocity.y < 0) {
            m_velocity.y = -2f;
            m_jumpCount = 0;
        }
    }

    private void HandleLook()
    {
        Vector2 lookInput = m_controls.Player.Look.ReadValue<Vector2>();

        float mouseX = lookInput.x * m_mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * m_mouseSensitivity * Time.deltaTime;

        // Rotate the body (left/right)
        transform.Rotate(Vector3.up * mouseX);

        // Calculate and clamp the camera pitch (up/down)
        m_xRotation -= mouseY;
        m_xRotation = Mathf.Clamp(m_xRotation, -90f, 90f);

        if (m_fpvCameraTransform != null) {
            m_fpvCameraTransform.localRotation = Quaternion.Euler(m_xRotation, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = m_controls.Player.Move.ReadValue<Vector2>();

        bool isSprinting = m_controls.Player.Sprint.IsPressed();
        float currentSpeed = isSprinting ? m_sprintSpeed : m_walkSpeed;

        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        m_characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    private void HandleGravity()
    {
        m_velocity.y += m_gravity * Time.deltaTime;

        m_characterController.Move(m_velocity * Time.deltaTime);
    }

    // Called when CharacterController collides with a collider (handles contact damage from non-trigger colliders)
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Check if the object we hit has a ContactDamageDealer
        if (hit.gameObject.TryGetComponent<ContactDamageDealer>(out var dealer)) {
            // Let the dealer handle the damage through its public method
            dealer.DealDamageTo(this);
        }
    }
}