using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 5f;
    public float attackRange = 2f;
    public float stoppingDistance = 1.8f; // Stop slightly before attack range
    
    [Header("Combat")]
    public float attackDamage = 15f;
    public float attackCooldown = 2f;
    public LayerMask playerLayer;
    
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private Health playerHealth;

    void Start()
    {
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
        }

        Rigidbody enemyRb = GetComponent<Rigidbody>();
        if (enemyRb != null)
        {
            enemyRb.constraints = RigidbodyConstraints.FreezeRotationX | 
                                RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Always face the player (smooth rotation)
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // State machine logic
        if (distance > stoppingDistance && !isAttacking)
        {
            // Move toward player
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
        else if (distance <= attackRange)
        {
            // Attack if cooldown passed
            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    void Attack()
    {
        isAttacking = true;
        Debug.Log("Enemy attacks player!");
        
        // Check if player is still in range (they might have dodged)
        float currentDistance = Vector3.Distance(transform.position, player.position);
        if (currentDistance <= attackRange && playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
        
        // Reset attack state after a brief delay (simulate attack animation)
        Invoke(nameof(ResetAttack), 0.5f);
    }

    void ResetAttack()
    {
        isAttacking = false;
    }

    // Visualize ranges in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}