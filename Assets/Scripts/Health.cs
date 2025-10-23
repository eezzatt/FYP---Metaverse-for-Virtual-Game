using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public bool canRegenerate = false;
    public float regenRate = 5f; // HP per second
    public float regenDelay = 3f; // Delay after taking damage
    
    [Header("Defense")]
    public float armor = 0f; // Damage reduction percentage (0-100)
    public float invincibilityDuration = 0.5f; // I-frames after hit
    
    [Header("Events")]
    public UnityEvent<float> OnHealthChanged; // Passes current health percentage (0-1)
    public UnityEvent OnDeath;
    public UnityEvent<float> OnDamageTaken; // Passes damage amount

    private float currentHealth;
    private bool isDead = false;
    private bool isInvincible = false;
    private float lastDamageTime;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(GetHealthPercentage());
    }

    void Update()
    {
        if (isDead || !canRegenerate) return;

        // Regenerate health after delay
        if (Time.time - lastDamageTime >= regenDelay && currentHealth < maxHealth)
        {
            Heal(regenRate * Time.deltaTime);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead || isInvincible) return;

        // Apply armor reduction
        float damageReduction = Mathf.Clamp(armor, 0f, 100f) / 100f;
        float actualDamage = amount * (1f - damageReduction);

        currentHealth -= actualDamage;
        currentHealth = Mathf.Max(currentHealth, 0f);
        lastDamageTime = Time.time;

        Debug.Log($"{gameObject.name} took {actualDamage:F1} damage (reduced by {armor}% armor). Remaining: {currentHealth:F1}/{maxHealth}");

        OnDamageTaken?.Invoke(actualDamage);
        OnHealthChanged?.Invoke(GetHealthPercentage());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Activate invincibility frames
            StartInvincibility();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        OnHealthChanged?.Invoke(GetHealthPercentage());
    }

    void StartInvincibility()
    {
        isInvincible = true;
        Invoke(nameof(EndInvincibility), invincibilityDuration);
    }

    void EndInvincibility()
    {
        isInvincible = false;
    }

    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log($"{gameObject.name} has died!");
        
        OnDeath?.Invoke();
        
        // Optional: Disable components instead of destroying
        // This is better for respawn systems
        DisableEntity();
    }

    void DisableEntity()
    {
        // Disable movement and combat scripts
        var combat = GetComponent<PlayerCombat>();
        if (combat) combat.enabled = false;

        var enemyController = GetComponent<EnemyController>();
        if (enemyController) enemyController.enabled = false;

        // You might want to play death animation here
        // animator.SetTrigger("Death");
    }

    // Public getters
    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public bool IsDead() => isDead;
    public bool IsInvincible() => isInvincible;

    // For respawn systems
    public void Respawn()
    {
        isDead = false;
        isInvincible = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(GetHealthPercentage());
        
        // Re-enable components
        var combat = GetComponent<PlayerCombat>();
        if (combat) combat.enabled = true;

        var enemyController = GetComponent<EnemyController>();
        if (enemyController) enemyController.enabled = true;
    }
}