using UnityEngine;

public class Player_ground_movement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float maxSpeed = 10f;



    private Rigidbody rb;
    //private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Get input (WASD or Arrow keys)

        float moveH = Input.GetAxis("Horizontal");
        float moveV = Input.GetAxis("Vertical");

        Vector3 fwd = this.transform.forward;
        Vector3 right = this.transform.right;
        fwd.y = 0f;
        right.y = 0f;
        fwd.Normalize();
        right.Normalize();

        // Movement direction relative to player orientation
        Vector3 move = (fwd * moveV + right * moveH).normalized;

        // Apply movement force
        if (move.magnitude > 0)
        {
            rb.AddForce(move * moveSpeed, ForceMode.Acceleration);
        }

        // Optional: limit max speed
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
        if (move.magnitude == 0)
        {
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x * 0.0f, v.y, v.z * 0.0f); // tweak 0.8f for smoother or faster stop
        }

        //// Jump
        //if (Input.GetButtonDown("Jump") && isGrounded)
        //{
        //    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        //    isGrounded = false;
        //}
    }

    // Simple ground check
    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Ground"))
    //        isGrounded = true;
    //}
}
