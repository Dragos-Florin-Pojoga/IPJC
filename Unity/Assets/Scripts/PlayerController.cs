using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerClean : MonoBehaviour
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

    private CharacterController m_characterController;
    private PlayerControls m_controls;

    private Vector3 m_velocity;
    private float m_xRotation = 0f;
    private int m_jumpCount = 0;

    private void Awake()
    {
        m_characterController = GetComponent<CharacterController>();

        m_controls = new PlayerControls();
        m_controls.Player.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (m_fpvCameraTransform != null) {
            m_fpvCameraTransform.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
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
}