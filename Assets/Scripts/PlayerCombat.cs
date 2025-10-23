using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat Stats")]
    public float attackRange = 2f;
    public float attackDamage = 20f;
    public float attackCooldown = 1f;
    public float attackAngle = 90f; // Cone in front of player
    
    [Header("Settings")]
    public LayerMask enemyLayer;
    public Transform attackPoint; // Optional: specific point to attack from (e.g., weapon tip)

    private float nextAttackTime = 0f;
    private int comboCount = 0;
    private float comboResetTime = 1.5f;
    private float lastAttackTime = 0f;

    void Update()
    {
        // Reset combo if too much time passed
        if (Time.time - lastAttackTime > comboResetTime)
        {
            comboCount = 0;
        }

        if (Time.time >= nextAttackTime)
        {
            if (Input.GetMouseButtonDown(0)) // Left click to attack
            {
                Attack();
                nextAttackTime = Time.time + attackCooldown;
                lastAttackTime = Time.time;
            }
        }
    }

    void Attack()
    {
        comboCount++;
        Debug.Log($"Player attacks! Combo: {comboCount}");
        
        // TODO: Trigger attack animation here
        // animator.SetTrigger("Attack");
        
        Vector3 attackOrigin = attackPoint != null ? attackPoint.position : transform.position;
        
        // Find all enemies in range
        Collider[] hitColliders = Physics.OverlapSphere(attackOrigin, attackRange, enemyLayer);

        foreach (Collider enemy in hitColliders)
        {
            // Check if enemy is in front of player (within attack cone)
            Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
            float angleToEnemy = Vector3.Angle(transform.forward, directionToEnemy);

            if (angleToEnemy < attackAngle / 2f)
            {
                Health enemyHealth = enemy.GetComponent<Health>();
                if (enemyHealth != null && !enemyHealth.IsDead())
                {
                    // Apply combo multiplier
                    float damage = attackDamage * (1f + (comboCount - 1) * 0.1f);
                    enemyHealth.TakeDamage(damage);
                    
                    Debug.Log($"Hit {enemy.name} for {damage} damage!");
                }
            }
        }
    }

    // Visualize attack range and cone
    void OnDrawGizmosSelected()
    {
        Vector3 origin = attackPoint != null ? attackPoint.position : transform.position;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRange);
        
        // Draw attack cone
        Gizmos.color = Color.yellow;
        Vector3 rightBoundary = Quaternion.Euler(0, attackAngle / 2f, 0) * transform.forward * attackRange;
        Vector3 leftBoundary = Quaternion.Euler(0, -attackAngle / 2f, 0) * transform.forward * attackRange;
        
        Gizmos.DrawLine(origin, origin + rightBoundary);
        Gizmos.DrawLine(origin, origin + leftBoundary);
    }
}