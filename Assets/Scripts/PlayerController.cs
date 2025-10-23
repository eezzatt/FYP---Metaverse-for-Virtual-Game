using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed = 150f;
    public float jumpForce = 5f;
    private int score = 0;
    private Vector3 lastPosition;

    private DataLogger logger;
    private Rigidbody rb;
    private bool isGrounded;

    private void Start()
    {
        lastPosition = transform.position;
        logger = FindFirstObjectByType<DataLogger>();
        logger.Log("Start", transform.position, score);
        rb = GetComponent<Rigidbody>();
        
        // FORCE constraints - clear first then set
        rb.constraints = RigidbodyConstraints.None;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        Debug.Log($"Rigidbody constraints applied: {rb.constraints}");
        
        // Prevent enemies from physically pushing the player
        IgnoreEnemyCollisions();
    }

    private void IgnoreEnemyCollisions()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Collider playerCollider = GetComponent<Collider>();
        
        foreach (GameObject enemy in enemies)
        {
            Collider enemyCollider = enemy.GetComponent<Collider>();
            if (enemyCollider != null && playerCollider != null)
            {
                Physics.IgnoreCollision(playerCollider, enemyCollider);
            }
        }
    }

    private void Update()
    {
        // Handle rotation in Update (not affected by physics collisions)
        float horizontal = Input.GetAxis("Horizontal");
        if (horizontal != 0f)
        {
            float turnAmount = horizontal * turnSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, turnAmount);
        }

        // Handle jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        // Re-check for new enemies spawned during gameplay
        if (Time.frameCount % 60 == 0)
        {
            IgnoreEnemyCollisions();
        }

        // Logging
        if (Vector3.Distance(transform.position, lastPosition) > 0.1f)
        {
            logger.Log("Move", transform.position, score);
            lastPosition = transform.position;
        }
    }

    private void FixedUpdate()
    {
        float vertical = Input.GetAxis("Vertical");

        // Move forward/back using Rigidbody
        if (vertical != 0f)
        {
            Vector3 forwardMovement = transform.forward * vertical * moveSpeed * Time.fixedDeltaTime;
            Vector3 newPosition = rb.position + forwardMovement;
            rb.MovePosition(newPosition);
        }
        
        // Force upright after physics
        ForceUpright();
    }

    private void ForceUpright()
    {
        // Get current rotation
        Vector3 euler = transform.eulerAngles;
        
        // Normalize angles to -180 to 180 range for easier checking
        float normalizedX = euler.x > 180 ? euler.x - 360 : euler.x;
        float normalizedZ = euler.z > 180 ? euler.z - 360 : euler.z;
        
        // If there's ANY tilt on X or Z axis, smoothly correct it
        if (Mathf.Abs(normalizedX) > 0.01f || Mathf.Abs(normalizedZ) > 0.01f)
        {
            // Target rotation (keep Y, zero out X and Z)
            Quaternion targetRotation = Quaternion.Euler(0f, euler.y, 0f);
            
            // Smoothly interpolate from current to target (adjust speed with the multiplier)
            Quaternion smoothRotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f);
            
            // Apply smoothly to Rigidbody
            rb.rotation = smoothRotation;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectible"))
        {
            score++;
            Destroy(other.gameObject);
            logger.Log("Collect", transform.position, score);
        }
    }
}