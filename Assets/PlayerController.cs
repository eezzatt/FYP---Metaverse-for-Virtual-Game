using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed = 150f;
    public float jumpForce = 5f; // Jump strength
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
        rb = GetComponent<Rigidbody>(); // get Rigidbody component
    }

    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 1. Rotate player left/right
        if (horizontal != 0f)
            transform.Rotate(Vector3.up, horizontal * turnSpeed * Time.deltaTime);

        // 2. Move forward/back relative to player
        Vector3 forwardMovement = transform.forward * vertical * moveSpeed * Time.deltaTime;
        transform.position += forwardMovement;

        // 3. Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false; // player is now in air
        }

        // 4. Logging
        if (Vector3.Distance(transform.position, lastPosition) > 0.1f)
        {
            logger.Log("Move", transform.position, score);
            lastPosition = transform.position;
        }
    }

    // Check if player is on the ground
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
