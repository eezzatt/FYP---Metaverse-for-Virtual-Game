using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    public Transform attackPoint;
    public float attackRange;
    public LayerMask enemyLayers;
    
    [Header("Dodge Settings")]
    public float dodgeDuration;
    public float dodgeDistance;
    
    [Header("Audio Feedback")]
    public AudioClip attackSound;        // Whoosh/swing sound
    public AudioClip hitSound;           // Impact sound when you hit enemy
    [Range(0f, 1f)]
    public float attackVolume = 0.7f;    // Volume control
    
    private AudioSource audioSource;
    private int attackDamage;
    private float attackCooldown;
    private float dodgeCooldown;

    private float nextAttackTime = 0f;
    private float nextDodgeTime = 0f;
    private bool isDodging = false;
    private FightingGameSession gameSession;
    private Rigidbody rb;
    private DifficultyLevel difficultyLevel;
    
    // Combo tracking
    private int currentCombo = 0;
    private float lastHitTime = 0f;
    private float comboResetTime = 2f; // Reset combo if no hit for 2 seconds
    private int comboThreshold = 2  ; // Minimum hits to count as combo

    void Start()
    {
        gameSession = FindFirstObjectByType<FightingGameSession>();
        rb = GetComponent<Rigidbody>();
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // Configure audio source for combat sounds
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.volume = attackVolume;
        
        if (gameSession == null)
        {
            Debug.LogWarning("PlayerCombat: No FightingGameSession found in scene");
        }
        
        if (rb == null)
        {
            Debug.LogWarning("PlayerCombat: No Rigidbody found - dodge mechanic won't work properly");
        }

        difficultyLevel = MainMenuManager.GetDifficulty();
        InitializePlayerStats(difficultyLevel);
        Debug.Log("Player Difficulty: " + difficultyLevel);
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
        // Play attack swing sound
        PlaySound(attackSound);
        
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
        
        // Play hit or miss sound
        if (hitSomething)
        {
            PlaySound(hitSound);
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

    void InitializePlayerStats(DifficultyLevel difficulty)
    {
        if (difficulty == DifficultyLevel.Easy)
        {
            attackDamage = 10;
            attackCooldown = 0.8f;
            dodgeCooldown = 0.5f;
        }
        else if (difficulty == DifficultyLevel.Medium)
        {
            attackDamage = 8;
            attackCooldown = 1f;
            dodgeCooldown = 1f;
        }
        else
        {
            attackDamage = 5;
            attackCooldown = 1.2f;
            dodgeCooldown = 1.5f;
        }
    }

    // ===== AUDIO FEEDBACK METHOD =====
    
    void PlaySound(AudioClip clip, float pitchVariation = 1.0f)
    {
        if (clip == null || audioSource == null) return;
        
        // Add slight pitch variation for more dynamic sound
        audioSource.pitch = pitchVariation + Random.Range(-0.1f, 0.1f);
        audioSource.PlayOneShot(clip, attackVolume);
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}