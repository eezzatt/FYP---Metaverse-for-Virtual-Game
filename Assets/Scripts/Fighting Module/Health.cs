using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;

    public Slider healthBar;
    public Image healthBarFill;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;

    // UnityEvent for CombatUIManager to subscribe to
    [System.NonSerialized]
    public UnityEvent<float> OnHealthChanged = new UnityEvent<float>();

    private FightingGameSession gameSession;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        
        gameSession = FindFirstObjectByType<FightingGameSession>();
        
        // Invoke initial health percentage
        OnHealthChanged.Invoke(GetHealthPercentage());
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        
        UpdateHealthBar();
        
        // Invoke health changed event
        OnHealthChanged.Invoke(GetHealthPercentage());

        // Notify game session if this is the player
        if (gameObject.CompareTag("Player") && gameSession != null)
        {
            gameSession.OnPlayerHit();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log($"{gameObject.name} has died!");

        // Notify game session if player died
        if (gameObject.CompareTag("Player") && gameSession != null)
        {
            gameSession.OnPlayerDeath();
        }

        DisableEntity();
    }

    void DisableEntity()
    {
        // Disable movement and combat
        var playerController = GetComponent<PlayerController>();
        if (playerController) playerController.enabled = false;

        var enemyController = GetComponent<EnemyController>();
        if (enemyController) enemyController.enabled = false;

        var playerCombat = GetComponent<PlayerCombat>();
        if (playerCombat) playerCombat.enabled = false;

        // Visual feedback - make gray and fall
        Renderer renderer = GetComponent<Renderer>();
        if (renderer)
        {
            renderer.material.color = Color.gray;
        }

        // Optional: Destroy after delay
        // Destroy(gameObject, 3f);
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            healthBar.value = healthPercent;

            if (healthBarFill != null)
            {
                healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
            }
        }
    }

    // ===== PUBLIC METHODS FOR CombatUIManager =====
    
    public bool IsDead()
    {
        return isDead;
    }

    public int GetHealth()
    {
        return currentHealth;
    }
    
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateHealthBar();
        
        // Invoke health changed event
        OnHealthChanged.Invoke(GetHealthPercentage());
    }
}