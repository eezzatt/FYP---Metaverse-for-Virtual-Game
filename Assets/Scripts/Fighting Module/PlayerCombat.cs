using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 10;
    public float attackCooldown = 0.5f;
    
    [Header("Attack Visual Feedback")]
    public GameObject attackVisualPrefab; // Optional: assign a particle effect
    public float attackAnimationDuration = 0.2f;
    
    private float nextAttackTime = 0f;
    private FightingGameSession gameSession;
    private bool isAttacking = false;
    private Vector3 originalScale;

    void Start()
    {
        gameSession = FindFirstObjectByType<FightingGameSession>();
        originalScale = transform.localScale;
        
        if (gameSession == null)
        {
            Debug.LogWarning("PlayerCombat: No FightingGameSession found in scene");
        }
    }

    void Update()
    {
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Attack();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    void Attack()
    {
        if (isAttacking) return;
        
        isAttacking = true;
        
        // Visual feedback: Quick punch forward motion
        StartCoroutine(AttackAnimation());
        
        // Spawn visual effect if assigned
        if (attackVisualPrefab != null && attackPoint != null)
        {
            GameObject effect = Instantiate(attackVisualPrefab, attackPoint.position, attackPoint.rotation);
            Destroy(effect, 1f);
        }
        
        // Detect enemies in range
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);
        
        bool hitSomething = false;

        foreach (Collider enemy in hitEnemies)
        {
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null && !enemyHealth.IsDead())
            {
                enemyHealth.TakeDamage(attackDamage);
                hitSomething = true;
                Debug.Log($"Hit {enemy.name} for {attackDamage} damage");
                
                // ADDED: Make enemy flash red when hit
                StartCoroutine(FlashRed(enemy.gameObject));
                
                // ADDED: Small knockback effect
                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 knockbackDirection = (enemy.transform.position - transform.position).normalized;
                    enemyRb.AddForce(knockbackDirection * 3f, ForceMode.Impulse);
                }
            }
        }

        // Report to game session
        if (gameSession != null)
        {
            gameSession.OnPlayerAttack(hitSomething);
        }
    }

    System.Collections.IEnumerator AttackAnimation()
    {
        // Quick forward lunge
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + transform.forward * 0.3f; // Lunge forward
        
        // Forward motion (first half)
        while (elapsed < attackAnimationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (attackAnimationDuration / 2);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            
            // Slight scale increase for impact
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.1f, t);
            yield return null;
        }
        
        // Return to original position (second half)
        elapsed = 0f;
        while (elapsed < attackAnimationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (attackAnimationDuration / 2);
            transform.position = Vector3.Lerp(targetPos, startPos, t);
            transform.localScale = Vector3.Lerp(originalScale * 1.1f, originalScale, t);
            yield return null;
        }
        
        // Ensure we're back at original state
        transform.position = startPos;
        transform.localScale = originalScale;
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

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}