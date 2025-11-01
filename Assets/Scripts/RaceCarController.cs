using UnityEngine;

public class RaceCarController : MonoBehaviour
{
    public float accelerationSpeed = 5f;
    public float maxSpeed =5f;  // Reduced from 30f
    public float turnSpeed = 100f;
    public float brakeForce = 10f;
    
    private Rigidbody rb;
    private float currentSpeed = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
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
            currentSpeed = Mathf.Max(currentSpeed - 2f * Time.fixedDeltaTime, 0);
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
        
        // NEW: Prevent tunneling by clamping velocity
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
}