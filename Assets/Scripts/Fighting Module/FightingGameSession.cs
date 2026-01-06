using UnityEngine;
using System.Collections.Generic;

public class FightingGameSession : MonoBehaviour
{
    [Header("Session Settings")]
    public int playerID;
    public DifficultyLevel currentDifficulty;
    
    [Header("References")]
    public GameObject player;
    public GameObject enemy;

    private float perfectDodgeWindow; // Time window after enemy attack starts

    // Session tracking
    private SessionData sessionData;
    private float sessionStartTime;
    private bool sessionActive = false;
    private bool sessionEnded = false;

    // Combat metrics
    private int playerDeaths = 0;
    private int combosExecuted = 0;
    private int perfectDodges = 0;
    private int hitsDealt = 0;
    private int hitsTaken = 0;
    private int totalAttacks = 0;
    private List<float> reactionTimes = new List<float>();

    // Enemy attack tracking
    private float lastEnemyAttackTime = 0f;
    private bool enemyIsAttacking = false;
    private bool dodgeCounted;

    void Start()
    {
        // Don't start session automatically
        // Will be started when enemy is spawned/activated
        initializeDodgeWindow(currentDifficulty);
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
        
        // Reset enemy attack flag after window expires
        if (enemyIsAttacking && Time.time - lastEnemyAttackTime > perfectDodgeWindow)
        {
            enemyIsAttacking = false;
        }
    }

    void StartSession()
    {
        if (sessionActive || sessionEnded) return; // Prevent multiple starts

        sessionActive = true;
        sessionStartTime = Time.time;
        sessionData = new SessionData("Fighting");
        sessionData.playerID = playerID;
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
            EndSession(true, victory);
        }
    }

        void OnApplicationQuit()
    {
        if (!sessionActive || sessionEnded) return;
        EndSession(false, false);
    }

    public void EndSession(bool completed, bool victory)
    {
        if (!sessionActive || sessionEnded) return; // Prevent multiple endings

        sessionEnded = true;
        sessionActive = false;

        // Calculate session metrics
        float duration = Time.time - sessionStartTime;
        float accuracy = totalAttacks > 0 ? (float)hitsDealt / totalAttacks : 0f;
        float avgReactionTime = reactionTimes.Count > 0 ? CalculateAverage(reactionTimes) : 0f;

        // Populate session data
        sessionData.sessionDuration = duration;
        sessionData.score = CalculateScore(victory, accuracy, duration);
        sessionData.deaths = playerDeaths;
        sessionData.completed = completed;

        // Add fighting-specific data
        sessionData.gameSpecificData["victory"] = victory;
        sessionData.gameSpecificData["combosExecuted"] = combosExecuted;
        sessionData.gameSpecificData["perfectDodges"] = perfectDodges;
        sessionData.gameSpecificData["hitsDealt"] = hitsDealt;
        sessionData.gameSpecificData["hitsTaken"] = hitsTaken;
        sessionData.gameSpecificData["playerAccuracy"] = accuracy;
        sessionData.gameSpecificData["avgReactionTime"] = avgReactionTime;

        // Save to CSV
        GameplayDataCollector.Instance.SaveSessionData(sessionData);

        Debug.Log($"Fighting Game Session ENDED - Victory: {victory} | Duration: {duration:F2}s | Accuracy: {accuracy:F2} | Perfect Dodges: {perfectDodges} | Combos: {combosExecuted} | Avg Reaction: {avgReactionTime:F3}s");
    }

    int CalculateScore(bool victory, float accuracy, float duration)
    {
        int baseScore = victory ? 1000 : 0;
        int accuracyBonus = Mathf.RoundToInt(accuracy * 500);
        int speedBonus = duration < 30 ? 300 : (duration < 60 ? 150 : 0);
        int comboBonus = combosExecuted * 50;
        int dodgeBonus = perfectDodges * 100;

        return baseScore + accuracyBonus + speedBonus + comboBonus + dodgeBonus;
    }

    float CalculateAverage(List<float> values)
    {
        if (values.Count == 0) return 0f;
        float sum = 0f;
        foreach (float value in values)
        {
            sum += value;
        }
        return sum / values.Count;
    }

    // ===== PUBLIC METHODS FOR COMBAT TRACKING =====

    public void OnPlayerAttack(bool hit)
    {
        if (!sessionActive) return;

        totalAttacks++;
        if (hit)
        {
            hitsDealt++;
        }
    }

    public void OnPlayerHit()
    {
        if (!sessionActive) return;
        hitsTaken++;
    }

    public void OnComboExecuted()
    {
        if (!sessionActive) return;
        combosExecuted++;
        Debug.Log($"[SESSION] Combo recorded! Total: {combosExecuted}");
    }

    public void OnPerfectDodge()
    {
        if (!sessionActive) return;
        perfectDodges++;
        Debug.Log($"[SESSION] Perfect dodge recorded! Total: {perfectDodges}");
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
        Debug.Log($"[SESSION] Reaction time recorded: {time:F3}s");
    }

    // Called when enemy starts attacking
    public void OnEnemyAttackStart()
    {
        if (!sessionActive) return;
        lastEnemyAttackTime = Time.time;
        enemyIsAttacking = true;
        dodgeCounted = false;
        Debug.Log("[SESSION] Enemy attack started - dodge window open!");
    }

    // Called by PlayerCombat when player dodges
    public void CheckPerfectDodge()
    {
        if (!sessionActive) return;
        
        // Check if dodge happened during enemy attack window
        if (enemyIsAttacking && !dodgeCounted)
        {
            float reactionTime = Time.time - lastEnemyAttackTime;
            
            if (reactionTime <= perfectDodgeWindow)
            {
                OnPerfectDodge();
                RecordReactionTime(reactionTime);
                dodgeCounted = true;
            }
        }
    }

    void initializeDodgeWindow(DifficultyLevel difficulty)
    {
        if (difficulty == DifficultyLevel.Easy)
            {
                perfectDodgeWindow = 3.0f;
            }
        else if (difficulty == DifficultyLevel.Medium)
            {
                perfectDodgeWindow = 2.0f;
            }
            else
            {
                perfectDodgeWindow = 1.0f;
            }
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