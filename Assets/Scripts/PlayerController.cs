using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float turnSpeed;
    public float jumpForce;
    private int score = 0;
    private Vector3 lastPosition;
    private Rigidbody rb;
    private bool isGrounded;

    private void Start()
    {
        lastPosition = transform.position;
        
        rb = GetComponent<Rigidbody>();
        
        rb.constraints = RigidbodyConstraints.None;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        Debug.Log($"Rigidbody constraints applied: {rb.constraints}");
        
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
        float horizontal = Input.GetAxis("Horizontal");
        if (horizontal != 0f)
        {
            float turnAmount = horizontal * turnSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, turnAmount);
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        if (Time.frameCount % 60 == 0)
        {
            IgnoreEnemyCollisions();
        }
    }

    private void FixedUpdate()
    {
        float vertical = Input.GetAxis("Vertical");

        if (vertical != 0f)
        {
            Vector3 forwardMovement = transform.forward * vertical * moveSpeed * Time.fixedDeltaTime;
            Vector3 newPosition = rb.position + forwardMovement;
            rb.MovePosition(newPosition);
        }
        
        ForceUpright();
    }

    private void ForceUpright()
    {
        Vector3 euler = transform.eulerAngles;
        
        float normalizedX = euler.x > 180 ? euler.x - 360 : euler.x;
        float normalizedZ = euler.z > 180 ? euler.z - 360 : euler.z;
        
        if (Mathf.Abs(normalizedX) > 0.01f || Mathf.Abs(normalizedZ) > 0.01f)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, euler.y, 0f);
            Quaternion smoothRotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f);
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
        }
    }
}