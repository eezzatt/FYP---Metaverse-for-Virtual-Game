using UnityEngine;

public class RaceCarController : MonoBehaviour
{
    private float accelerationSpeed;
    private float maxSpeed;
    private float turnSpeed = 100f;
    private float brakeForce = 10f;
    
    private Rigidbody rb;
    private RacingGameSession gameSession; // FIXED: Only one session variable
    internal float currentSpeed = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        gameSession = FindFirstObjectByType<RacingGameSession>();
        
        if (gameSession != null)
        {
            DiffcultySetter(gameSession.currentDifficulty);
        }
        else
        {
            Debug.LogWarning("RaceCarController: No RacingGameSession found - using default settings");
            accelerationSpeed = 20f;
            maxSpeed = 35f;
        }
    }

    private void FixedUpdate()
    {
        // FIXED: Check gameSession instead of session
        if (gameSession != null && !gameSession.isCountingDown)
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

        // FIXED: Prevent tunneling by clamping velocity
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // FIXED: Use gameSession instead of session
        if (gameSession != null)
        {
            gameSession.OnCollision(collision);
        }
    }
    
    private void DiffcultySetter(DifficultyLevel difficulty)
    {
        if (difficulty == DifficultyLevel.Easy)
        {
            accelerationSpeed = 15f;
            maxSpeed = 30f;
        }
        else if (difficulty == DifficultyLevel.Medium)
        {
            accelerationSpeed = 20f;
            maxSpeed = 40f;
        }
        else
        {
            accelerationSpeed = 25f;
            maxSpeed = 45f;
        }
    }
}