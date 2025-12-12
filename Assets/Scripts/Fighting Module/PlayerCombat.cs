using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 10;
    public float attackCooldown = 0.5f;
    
    [Header("Dodge Settings")]
    public float dodgeDuration = 0.3f;
    public float dodgeCooldown = 1f;
    public float dodgeDistance = 3f;
    
    private float nextAttackTime = 0f;
    private float nextDodgeTime = 0f;
    private bool isDodging = false;
    private FightingGameSession gameSession;
    private Rigidbody rb;
    
    // Combo tracking
    private int currentCombo = 0;
    private float lastHitTime = 0f;
    private float comboResetTime = 2f; // Reset combo if no hit for 2 seconds
    private int comboThreshold = 2  ; // Minimum hits to count as combo

    void Start()
    {
        gameSession = FindFirstObjectByType<FightingGameSession>();
        rb = GetComponent<Rigidbody>();
        
        if (gameSession == null)
        {
            Debug.LogWarning("PlayerCombat: No FightingGameSession found in scene");
        }
        
        if (rb == null)
        {
            Debug.LogWarning("PlayerCombat: No Rigidbody found - dodge mechanic won't work properly");
        }
    }

    void Update()
    {
        // Reset combo if too much time has passed
        if (currentCombo > 0 && Time.time - lastHitTime > comboResetTime)
        {
            ResetCombo();
        }
        
        // Attack input
        if (Time.time >= nextAttackTime && !isDodging)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Attack();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
        
        // Dodge input - Note: Changed to LeftShift so it doesn't conflict with jump
        if (Input.GetKeyDown(KeyCode.V))
        {   
            if (Time.time >= nextDodgeTime)
            {
                StartDodge();
                nextDodgeTime = Time.time + dodgeCooldown;
                Debug.Log("Dodge executed!");
            }
            else
            {
                Debug.Log("Dodge on cooldown");
            }
        }
    }

    void Attack()
    {
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
                
                // Track combo
                currentCombo++;
                lastHitTime = Time.time;
                
                Debug.Log($"Hit {enemy.name} for {attackDamage} damage | Combo: {currentCombo}");
                
                // Report combo to session if threshold reached
                if (currentCombo >= comboThreshold)
                {
                    if (gameSession != null)
                    {
                        gameSession.OnComboExecuted();
                    }
                    Debug.Log($"COMBO x{currentCombo}!");
                }
            }
        }

        // Report to game session
        if (gameSession != null)
        {
            gameSession.OnPlayerAttack(hitSomething);
        }
    }

    void StartDodge()
    {
        if (rb == null) return;
        
        isDodging = true;
        
        // Quick dash in movement direction (or forward if standing still)
        Vector3 dodgeDirection = transform.forward;
        
        // Check for movement input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        if (horizontal != 0 || vertical != 0)
        {
            dodgeDirection = (transform.forward * vertical + transform.right * horizontal).normalized;
        }
        
        // Apply dodge force
        rb.AddForce(dodgeDirection * dodgeDistance, ForceMode.VelocityChange);
        
        // End dodge after duration
        Invoke(nameof(EndDodge), dodgeDuration);

        // Check for perfect dodge (if enemy was attacking recently)
        if (gameSession != null)
        {
            gameSession.CheckPerfectDodge();
        }
    }

    void EndDodge()
    {
        isDodging = false;
    }

    void ResetCombo()
    {
        if (currentCombo > 0)
        {
            Debug.Log($"Combo reset at {currentCombo} hits");
        }
        currentCombo = 0;
    }

    // Called by Health.cs when player takes damage
    public void OnPlayerTakeDamage()
    {
        ResetCombo();
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}