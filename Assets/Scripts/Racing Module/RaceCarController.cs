using UnityEngine;

public class RaceCarController : MonoBehaviour
{
    public float accelerationSpeed = 20f;
    public float maxSpeed = 20f;  // Reduced from 30f
    public float turnSpeed = 100f;
    public float brakeForce = 10f;
    
    private Rigidbody rb;
    private RacingGameSession session;
    private float currentSpeed = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // FIXED: Use correct API for Unity version
        #if UNITY_2023_1_OR_NEWER
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        #else
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        #endif

        session = FindFirstObjectByType<RacingGameSession>();
    }

    private void FixedUpdate()
    {
        if (!session.isCountingDown)
        {
            // Get input
            float moveInput = Input.GetAxis("Vertical");
            float turnInput = Input.GetAxis("Horizontal");

            // Acceleration
            if (moveInput > 0)
            {
                currentSpeed = Mathf.Min(currentSpeed + accelerationSpeed * Time.fixedDeltaTime, maxSpeed);
            }
            // Braking
            else if (moveInput < 0)
            {
                currentSpeed = Mathf.Max(currentSpeed - brakeForce * Time.fixedDeltaTime, 0);
            }
            // Natural slowdown
            else
            {
                currentSpeed = Mathf.Max(currentSpeed - 5f * Time.fixedDeltaTime, 0);
            }

            
            // Move forward
            Vector3 forwardMovement = transform.forward * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + forwardMovement);

            // Turn (only when moving)
            if (currentSpeed > 0.1f && turnInput != 0)
            {
                float turnAmount = turnInput * turnSpeed * Time.fixedDeltaTime;
                Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
                rb.MoveRotation(rb.rotation * turnRotation);
            }
        }
        

        // FIXED: Prevent tunneling by clamping velocity (Unity version compatible)
        #if UNITY_2023_1_OR_NEWER
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
        #else
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
        #endif
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // FIXED: Use FindObjectOfType instead of FindFirstObjectByType
        if (session != null)
        {
            session.OnCollision(collision);
        }
    }
}