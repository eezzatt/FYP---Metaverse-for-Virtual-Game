using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    private int maxHealth;
    private int currentHealth;
    internal bool isDead = false;

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
        gameSession = FindFirstObjectByType<FightingGameSession>();
        InitializeHealth(gameSession.currentDifficulty);

        currentHealth = maxHealth;
        UpdateHealthBar();

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
        if (gameObject.CompareTag("Player"))
        {
            if (gameSession != null)
            {
                gameSession.OnPlayerHit();
            }
            
            // Reset player's combo when they take damage
            PlayerCombat playerCombat = GetComponent<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.OnPlayerTakeDamage();
            }
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
        if (gameObject.CompareTag("Player")) 
        {
            // Disable movement and combat
            var playerController = GetComponent<PlayerController>();
            if (playerController) playerController.enabled = false;

            var playerCombat = GetComponent<PlayerCombat>();
            if (playerCombat) playerCombat.enabled = false;

            GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");
            var enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController) enemyController.enabled = false;
        }
        else
        {
            // Disable movement and combat
            var enemyController = GetComponent<EnemyController>();
            if (enemyController) enemyController.enabled = false;

            GameObject player = GameObject.FindGameObjectWithTag("Player");

            // Disable movement and combat
            var playerController = player.GetComponent<PlayerController>();
            if (playerController) playerController.enabled = false;

            var playerCombat = player.GetComponent<PlayerCombat>();
            if (playerCombat) playerCombat.enabled = false;
        }

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

    void InitializeHealth(DifficultyLevel difficulty)
    {
        if (gameObject.name.ToLower().Contains("player"))
        {
            if(difficulty == DifficultyLevel.Easy)
            {
                maxHealth = 120;
            }
            else if (difficulty == DifficultyLevel.Medium)
            {
                maxHealth = 100;
            }
            else
            {
                maxHealth = 80;
            }
        }
        else
        {
            maxHealth = 100;
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