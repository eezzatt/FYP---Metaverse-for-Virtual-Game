using UnityEngine;
using System.Collections.Generic;

public class FightingGameSession : MonoBehaviour
{
    [Header("Session Settings")]
    public DifficultyLevel currentDifficulty = DifficultyLevel.Medium;
    
    [Header("References")]
    public GameObject player;
    public GameObject enemy;

    // Session tracking
    private SessionData sessionData;
    private float sessionStartTime;
    private bool sessionActive = false;
    private bool sessionEnded = false;

    // Combat metrics
    private int playerDeaths = 0;
    private int combosExecuted = 0;
    private int perfectDodges = 0;
    private int hitsLanded = 0;
    private int enemyHitsTaken = 0;
    private int totalAttacks = 0;
    private List<float> reactionTimes = new List<float>();

    void Start()
    {
        // Don't start session automatically
        // Will be started when enemy is spawned/activated
    }

    void Update()
    {
        // Check if we should start the session
        if (!sessionActive && !sessionEnded && enemy != null && player != null)
        {
            // Check if both player and enemy are alive
            Health playerHealth = player.GetComponent<Health>();
            Health enemyHealth = enemy.GetComponent<Health>();

            if (playerHealth != null && !playerHealth.IsDead() && 
                enemyHealth != null && !enemyHealth.IsDead())
            {
                StartSession();
            }
        }

        // Check for session end conditions
        if (sessionActive && !sessionEnded)
        {
            CheckSessionEnd();
        }
    }

    void StartSession()
    {
        if (sessionActive || sessionEnded) return; // Prevent multiple starts

        sessionActive = true;
        sessionStartTime = Time.time;
        sessionData = new SessionData("Fighting");
        sessionData.difficultyLevel = currentDifficulty;

        Debug.Log($"Fighting Game Session STARTED - Difficulty: {currentDifficulty}");
    }

    void CheckSessionEnd()
    {
        if (player == null || enemy == null) return;

        Health playerHealth = player.GetComponent<Health>();
        Health enemyHealth = enemy.GetComponent<Health>();

        if (playerHealth == null || enemyHealth == null) return;

        // End session if either player or enemy is dead
        if (playerHealth.IsDead() || enemyHealth.IsDead())
        {
            bool victory = enemyHealth.IsDead();
            EndSession(victory);
        }
    }

    public void EndSession(bool victory)
    {
        if (!sessionActive || sessionEnded) return; // Prevent multiple endings

        sessionEnded = true;
        sessionActive = false;

        // Calculate session metrics
        float duration = Time.time - sessionStartTime;
        float accuracy = totalAttacks > 0 ? (float)hitsLanded / totalAttacks : 0f;
        float avgReactionTime = reactionTimes.Count > 0 ? reactionTimes.Average() : 0f;

        // Populate session data
        sessionData.sessionDuration = duration;
        sessionData.score = CalculateScore(victory, accuracy, duration);
        sessionData.deaths = victory ? 0 : 1;
        sessionData.completed = victory;

        // Add fighting-specific data
        sessionData.gameSpecificData["victory"] = victory;
        sessionData.gameSpecificData["combosExecuted"] = combosExecuted;
        sessionData.gameSpecificData["perfectDodges"] = perfectDodges;
        sessionData.gameSpecificData["hitsLanded"] = hitsLanded;
        sessionData.gameSpecificData["enemyHitsTaken"] = enemyHitsTaken;
        sessionData.gameSpecificData["playerAccuracy"] = accuracy;
        sessionData.gameSpecificData["avgReactionTime"] = avgReactionTime;

        // Save to CSV
        GameplayDataCollector.Instance.SaveSessionData(sessionData);

        Debug.Log($"Fighting Game Session ENDED - Victory: {victory} | Duration: {duration:F2}s | Accuracy: {accuracy:F2}");
    }

    int CalculateScore(bool victory, float accuracy, float duration)
    {
        int baseScore = victory ? 1000 : 0;
        int accuracyBonus = Mathf.RoundToInt(accuracy * 500);
        int speedBonus = duration < 30 ? 300 : (duration < 60 ? 150 : 0);
        int comboBonus = combosExecuted * 50;

        return baseScore + accuracyBonus + speedBonus + comboBonus;
    }

    // Public methods for combat tracking
    public void OnPlayerAttack(bool hit)
    {
        if (!sessionActive) return;

        totalAttacks++;
        if (hit)
        {
            hitsLanded++;
        }
    }

    public void OnPlayerHit()
    {
        if (!sessionActive) return;
        enemyHitsTaken++;
    }

    public void OnComboExecuted()
    {
        if (!sessionActive) return;
        combosExecuted++;
    }

    public void OnPerfectDodge()
    {
        if (!sessionActive) return;
        perfectDodges++;
    }

    public void OnPlayerDeath()
    {
        if (!sessionActive) return;
        playerDeaths++;
    }

    public void RecordReactionTime(float time)
    {
        if (!sessionActive) return;
        reactionTimes.Add(time);
    }

    // ADDED: Track when enemy starts attacking (for reaction time measurement)
    private float lastEnemyAttackTime = 0f;
    
    public void OnEnemyAttackStart()
    {
        if (!sessionActive) return;
        lastEnemyAttackTime = Time.time;
        // Could be used to measure player reaction time if they dodge/block
    }
}

// Extension method to calculate average
public static class ListExtensions
{
    public static float Average(this List<float> list)
    {
        if (list.Count == 0) return 0f;
        float sum = 0f;
        foreach (float value in list)
        {
            sum += value;
        }
        return sum / list.Count;
    }
}