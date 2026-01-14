using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    
    [Header("Movement")]
    public float rotationSpeed = 5f;
    public float attackRange = 2f;
    public float stoppingDistance = 1.8f;
    
    [Header("Combat")]
    public LayerMask playerLayer;
    
    private float moveSpeed;
    private float attackWindupDuration;
    private float attackRecoveryDuration;
    private float attackDamage;
    private float attackCooldown;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private Health playerHealth;
    private Vector3 originalScale;
    private Coroutine currentAttackCoroutine; // Track the attack coroutine
    private Rigidbody rb;
    private FightingGameSession gameSession;

    void Start()
    {
        gameSession = FindFirstObjectByType<FightingGameSession>();
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
        }

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                                RigidbodyConstraints.FreezeRotationZ;
        }
        
        originalScale = transform.localScale;
        InitializeEnemy(gameSession.currentDifficulty);
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
            if (Time.time >= nextAttackTime && !isAttacking)
            {
                currentAttackCoroutine = StartCoroutine(AttackSequence());
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    System.Collections.IEnumerator AttackSequence()
    {
        isAttacking = true;
        
        // Notify session that enemy is attacking
        FightingGameSession session = FindFirstObjectByType<FightingGameSession>();
        if (session != null)
        {
            session.OnEnemyAttackStart();
        }
        
        // WINDUP PHASE
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        
        // Pull back and change color to show attack coming
        while (elapsed < attackWindupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / attackWindupDuration;
            
            // Pull back slightly
            Vector3 pullBackPos = startPos - transform.forward * 0.2f;
            transform.position = Vector3.Lerp(startPos, pullBackPos, t);
            
            // Scale up slightly
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.5f, t);
            
            yield return null;
        }
        
        // ATTACK PHASE - Lunge forward
        Debug.Log("Enemy attacks player!");
        
        elapsed = 0f;
        Vector3 lungeTarget = startPos + transform.forward * 0.5f;
        
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.1f;
            transform.position = Vector3.Lerp(transform.position, lungeTarget, t);
            yield return null;
        }
        
        // Check if player is still in range
        float currentDistance = Vector3.Distance(transform.position, player.position);
        if (currentDistance <= attackRange && playerHealth != null)
        {
            playerHealth.TakeDamage((int)attackDamage);

            // ADDED: Knockback the player with upward pop
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 knockbackDirection = (player.position - transform.position).normalized;
                knockbackDirection.y = 0; // Keep horizontal
                
                // Apply force with both horizontal and upward components
                Vector3 knockbackForce = knockbackDirection * 10f + Vector3.up * 10f;
                playerRb.AddForce(knockbackForce, ForceMode.Impulse);
            }
        }
        
        // RECOVERY PHASE
        elapsed = 0f;
        while (elapsed < attackRecoveryDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / attackRecoveryDuration;
            
            transform.position = Vector3.Lerp(transform.position, startPos, t);
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t);
            
            yield return null;
        }
        
        // Reset to original state
        transform.position = startPos;
        transform.localScale = originalScale;
        
        isAttacking = false;
    }

    // PUBLIC METHOD: Called when enemy takes damage (add this to Health.cs)
    public void OnTakeDamage(int damage, Vector3 attackerPosition)
    {
        // Interrupt current attack if in progress
        if (isAttacking && currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
            isAttacking = false;
            
            // Reset visual state
            transform.localScale = originalScale;
        }
        
        // Apply knockback - POP UP AND BACK!
        if (rb != null)
        {
            Vector3 knockbackDirection = (transform.position - attackerPosition).normalized;
            knockbackDirection.y = 0; // Keep horizontal direction
            
            // Apply force with both horizontal and upward components
            Vector3 knockbackForce = knockbackDirection * 20f + Vector3.up * 20f; // Horizontal + upward
            rb.AddForce(knockbackForce, ForceMode.Impulse);
        }
    }

    void InitializeEnemy(DifficultyLevel difficulty)
    {
        if (difficulty == DifficultyLevel.Easy)
        {
            moveSpeed = 4f;
            attackDamage = 8;
            attackCooldown = 1.5f;
            attackWindupDuration = 1.5f;
            attackRecoveryDuration = 1.5f;
        }
        else if (difficulty == DifficultyLevel.Medium)
        {
            moveSpeed = 5f;
            attackDamage = 10;
            attackCooldown = 0.7f;
            attackWindupDuration = 0.7f;
            attackRecoveryDuration = 0.7f;
        }
        else
        {
            moveSpeed = 5.5f;
            attackDamage = 10;
            attackCooldown = 0.5f;
            attackWindupDuration = 0.5f;
            attackRecoveryDuration = 0.5f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}