using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro support

public class CombatUIManager : MonoBehaviour
{
    [Header("Player Health")]
    public Health playerHealth;
    public Image playerHealthFill;
    public TMP_Text playerHealthText; // Changed to TextMeshPro
    
    [Header("Enemy Health")]
    public Health enemyHealth;
    public Image enemyHealthFill;
    public TMP_Text enemyHealthText; // Changed to TextMeshPro
    
    [Header("Colors")]
    public bool useFixedColors = false; // Toggle for fixed vs dynamic colors
    public Color fixedPlayerColor = Color.green;
    public Color fixedEnemyColor = Color.red;
    
    public Color fullHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;

    void Start()
    {
        // Subscribe to player health changes
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(UpdatePlayerHealth);
            UpdatePlayerHealth(playerHealth.GetHealthPercentage());
        }

        // Subscribe to enemy health changes
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged.AddListener(UpdateEnemyHealth);
            UpdateEnemyHealth(enemyHealth.GetHealthPercentage());
        }
    }

    void UpdatePlayerHealth(float healthPercentage)
    {
        if (playerHealthFill != null)
        {
            playerHealthFill.fillAmount = healthPercentage;
            
            // Use fixed color or dynamic gradient
            if (useFixedColors)
            {
                playerHealthFill.color = fixedPlayerColor;
            }
            else
            {
                playerHealthFill.color = GetHealthColor(healthPercentage);
            }
        }

        if (playerHealthText != null && playerHealth != null)
        {
            playerHealthText.text = $"{Mathf.Ceil(playerHealth.GetHealth())} / {playerHealth.GetMaxHealth()}";
        }
    }

    void UpdateEnemyHealth(float healthPercentage)
    {
        if (enemyHealthFill != null)
        {
            enemyHealthFill.fillAmount = healthPercentage;
            
            // Use fixed color or dynamic gradient
            if (useFixedColors)
            {
                enemyHealthFill.color = fixedEnemyColor;
            }
            else
            {
                enemyHealthFill.color = GetHealthColor(healthPercentage);
            }
        }

        if (enemyHealthText != null && enemyHealth != null)
        {
            enemyHealthText.text = $"{Mathf.Ceil(enemyHealth.GetHealth())} / {enemyHealth.GetMaxHealth()}";
        }
    }

    Color GetHealthColor(float percentage)
    {
        if (percentage > 0.6f)
        {
            // Green to yellow (60-100%)
            return Color.Lerp(midHealthColor, fullHealthColor, (percentage - 0.6f) / 0.4f);
        }
        else
        {
            // Red to yellow (0-60%)
            return Color.Lerp(lowHealthColor, midHealthColor, percentage / 0.6f);
        }
    }
}