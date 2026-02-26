using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class FightingGameSession : MonoBehaviour
{
    [Header("Session Settings")]
    [Tooltip("If true, use MainMenuManager values. If false, use Inspector values below")]
    public bool useMainMenuSettings = true;
    
    [Tooltip("Fallback player ID if not using main menu")]
    public int fallbackPlayerID = 1;
    
    [Tooltip("Fallback difficulty if not using main menu")]
    public DifficultyLevel fallbackDifficulty = DifficultyLevel.Medium;
    
    [Header("References")]
    public GameObject player;
    public GameObject enemy;

    [Header("UI References")]
    [Tooltip("Panel showing game controls")]
    public GameObject controlsPanel;
    public TMP_Text controlsText;
    public Button controlsOkayButton;
    
    [Tooltip("Panel showing countdown")]
    public GameObject countdownPanel;
    public TMP_Text countdownText;
    
    [Tooltip("Panel showing results")]
    public GameObject resultsPanel;
    public TMP_Text resultsTitleText;
    public TMP_Text durationText;
    public TMP_Text accuracyText;
    public TMP_Text combosText;
    public TMP_Text dodgesText;
    public TMP_Text hitsDealtText;
    public TMP_Text hitsTakenText;
    public TMP_Text scoreText;
    public Button XAIRecommendationButton;

    // Active session settings
    private int playerID;
    public DifficultyLevel currentDifficulty { get; private set; }

    private float perfectDodgeWindow;

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

    // Countdown
    private bool countdownActive = false;
    private float countdownTimer = 3f;

    void Start()
    {
        // Load settings from MainMenuManager or use fallback
        LoadSessionSettings();
        
        // Initialize dodge window based on difficulty
        initializeDodgeWindow(currentDifficulty);
        
        // Setup button listeners
        if (controlsOkayButton != null)
            controlsOkayButton.onClick.AddListener(OnControlsOkayClicked);
        
        if (XAIRecommendationButton != null)
            XAIRecommendationButton.onClick.AddListener(OnXAIRecommendationClicked);
        
        // Show controls panel and pause game
        ShowControlsPanel();
        
        Debug.Log($"Fighting Game Session initialized - Player ID: {playerID}, Difficulty: {currentDifficulty}");
    }


    void Update()
    {
        // Handle countdown
        if (countdownActive)
        {
            UpdateCountdown();
            return; // Don't run game logic during countdown
        }

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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnApplicationQuit();
        }
    }

    void LoadSessionSettings()
    {
        if (useMainMenuSettings)
        {
            // Try to load from MainMenuManager
            playerID = MainMenuManager.GetPlayerID();
            currentDifficulty = MainMenuManager.GetDifficulty();
            
            // Validation: If MainMenuManager wasn't used (values are 0), use fallback
            if (playerID == 0)
            {
                Debug.LogWarning("MainMenuManager values not found. Using fallback values.");
                playerID = fallbackPlayerID;
                currentDifficulty = fallbackDifficulty;
            }
        }
        else
        {
            // Use Inspector values
            playerID = fallbackPlayerID;
            currentDifficulty = fallbackDifficulty;
        }
    }

    void ShowControlsPanel()
    {
        HideAllPanels();
        
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
        }
        
        if (controlsText != null)
        {
            controlsText.text = @"<size=50><b>FIGHTING GAME CONTROLS</b></size>

<b>MOVEMENT:</b>
• W / Up Arrow - Move Forward
• S / Down Arrow - Move Backward
• A / Left Arrow - Turn Left
• D / Right Arrow - Turn Right
• Space - Jump

<b>COMBAT:</b>
• Left Mouse Button - Attack
• V Key - Dodge

<b>OBJECTIVE:</b>
Defeat the enemy by reducing their health to zero!
Land combos for bonus points.
Perfect dodges earn extra score.

<b>TIP:</b>
Watch for the enemy's attack windup to time your dodges perfectly!";
        }
        
        SetGamePaused(true);
    }

    void OnControlsOkayClicked()
    {
        HideAllPanels();
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(true);
        }
        Debug.Log("Starting countdown...");
        StartCountdown();
    }

    void StartCountdown()
    {
        countdownTimer = 3f;
        countdownActive = true;
        SetGamePaused(true);
    }


    void UpdateCountdown()
    {
        countdownTimer -= Time.unscaledDeltaTime;

        if (countdownText != null)
        {
            if (countdownTimer > 2f)
            {
                countdownText.text = "3";
                countdownText.color = Color.red;
            }
            else if (countdownTimer > 1f)
            {
                countdownText.text = "2";
                countdownText.color = Color.yellow;
            }
            else if (countdownTimer > 0f)
            {
                countdownText.text = "1";
                countdownText.color = Color.green;
            }
            else
            {
                countdownText.text = "GO!";
                countdownText.color = Color.green;
                countdownActive = false;
                Invoke(nameof(StartGame), 1f);
            }
        }
    }

    void StartGame()
    {       
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }
        
        SetGamePaused(false);
        Debug.Log("Game started!");
    }

    void StartSession()
    {
        if (sessionActive || sessionEnded) return;

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
        if (!sessionActive || sessionEnded) return;

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

        // Show results panel
        ShowResults(completed, victory, duration, accuracy);
    }

    void ShowResults(bool completed, bool victory, float duration, float accuracy)
    {
        HideAllPanels();
        
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
            bool wasNextRound = MainMenuManager.GetIsNextRound();
            MainMenuManager.SetIsNextRound(false); // reset before any reads below is wrong — move here but use wasNextRound

            if (XAIRecommendationButton != null)
                XAIRecommendationButton.gameObject.SetActive(!wasNextRound);
        }

        if (resultsTitleText != null)
        {
            if (completed)
            {
                resultsTitleText.text = victory ? "VICTORY!" : "DEFEAT";
                resultsTitleText.color = victory ? Color.green : Color.red;
            }
            else
            {
                resultsTitleText.text = "DNF";
                resultsTitleText.color = Color.grey;
            }
        }

        if (durationText != null)
            durationText.text = $"Duration: {duration:F1}s";

        if (accuracyText != null)
            accuracyText.text = $"Accuracy: {accuracy:P0}";

        if (combosText != null)
            combosText.text = $"Combos: {combosExecuted}";

        if (dodgesText != null)
            dodgesText.text = $"Perfect Dodges: {perfectDodges}";

        if (hitsDealtText != null)
            hitsDealtText.text = $"Hits Dealt: {hitsDealt}";

        if (hitsTakenText != null)
            hitsTakenText.text = $"Hits Taken: {hitsTaken}";

        if (scoreText != null)
            scoreText.text = $"Score: {sessionData.score}";

        SetGamePaused(true);
    }

    void OnXAIRecommendationClicked()
    {
        HideAllPanels();
        Debug.Log("Displaying XAI Recommendation");
        GetComponent<MLDifficultyClient>()?.RequestPrediction(sessionData);
    }

    void HideAllPanels()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
    }

    void SetGamePaused(bool paused)
    {        
        // Disable/enable player and enemy controllers
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
            
            if (playerController != null) playerController.enabled = !paused;
            if (playerCombat != null) playerCombat.enabled = !paused;
        }

        if (enemy != null)
        {
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null) enemyController.enabled = !paused;
        }
    }

    void BackToLobby()
    {
        SceneManager.LoadScene("MainMenu");
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

    public void OnEnemyAttackStart()
    {
        if (!sessionActive) return;
        lastEnemyAttackTime = Time.time;
        enemyIsAttacking = true;
        dodgeCounted = false;
        Debug.Log("[SESSION] Enemy attack started - dodge window open!");
    }

    public void CheckPerfectDodge()
    {
        if (!sessionActive) return;
        
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