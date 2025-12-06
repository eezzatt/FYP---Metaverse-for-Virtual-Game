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
    
    [Header("Animation")]
    public float attackWindupDuration = 0.3f; // Time before attack lands
    public float attackRecoveryDuration = 0.2f; // Time after attack
    
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private Health playerHealth;
    private Vector3 originalScale;

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
        
        originalScale = transform.localScale;
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
                StartCoroutine(AttackSequence());
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
        
        // WINDUP PHASE - Telegraph the attack
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Color startColor = GetComponent<Renderer>()?.material.color ?? Color.white;
        Renderer renderer = GetComponent<Renderer>();
        
        // Pull back and change color to show attack coming
        while (elapsed < attackWindupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / attackWindupDuration;
            
            // Pull back slightly
            Vector3 pullBackPos = startPos - transform.forward * 0.2f;
            transform.position = Vector3.Lerp(startPos, pullBackPos, t);
            
            // Flash orange/yellow to telegraph attack
            if (renderer != null)
            {
                renderer.material.color = Color.Lerp(startColor, Color.yellow, Mathf.PingPong(t * 4, 1));
            }
            
            // Scale up slightly
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.2f, t);
            
            yield return null;
        }
        
        // ATTACK PHASE - Lunge forward
        Debug.Log("Enemy attacks player!");
        
        elapsed = 0f;
        Vector3 lungeTarget = startPos + transform.forward * 0.5f;
        
        while (elapsed < 0.1f) // Quick lunge
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.1f;
            transform.position = Vector3.Lerp(transform.position, lungeTarget, t);
            yield return null;
        }
        
        // Check if player is still in range (they might have dodged during windup)
        float currentDistance = Vector3.Distance(transform.position, player.position);
        if (currentDistance <= attackRange && playerHealth != null)
        {
            playerHealth.TakeDamage((int)attackDamage);
            
            // Visual feedback on player
            StartCoroutine(FlashRed(player.gameObject));
        }
        
        // RECOVERY PHASE - Return to normal
        elapsed = 0f;
        while (elapsed < attackRecoveryDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / attackRecoveryDuration;
            
            transform.position = Vector3.Lerp(transform.position, startPos, t);
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t);
            
            if (renderer != null)
            {
                renderer.material.color = Color.Lerp(renderer.material.color, startColor, t);
            }
            
            yield return null;
        }
        
        // Ensure we're back to original state
        transform.position = startPos;
        transform.localScale = originalScale;
        if (renderer != null)
        {
            renderer.material.color = startColor;
        }
        
        isAttacking = false;
    }

    System.Collections.IEnumerator FlashRed(GameObject target)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null) yield break;
        
        Color originalColor = renderer.material.color;
        renderer.material.color = Color.red;
        
        yield return new WaitForSeconds(0.1f);
        
        renderer.material.color = originalColor;
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