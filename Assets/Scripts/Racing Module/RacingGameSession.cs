using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class RacingGameSession : MonoBehaviour
{
    [Header("Session Settings")]
    [Tooltip("If true, use MainMenuManager values. If false, use Inspector values below")]
    public bool useMainMenuSettings = true;
    
    [Tooltip("Fallback player ID if not using main menu")]
    public int fallbackPlayerID = 1;
    
    [Tooltip("Fallback difficulty if not using main menu")]
    public DifficultyLevel fallbackDifficulty = DifficultyLevel.Medium;
    
    [Tooltip("Number of laps to complete")]
    public int totalLaps = 3;

    [Header("References")]
    public GameObject racecar;
    public GameObject checkpoint; // Single checkpoint reference

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
    public TMP_Text lapsCompletedText;
    public TMP_Text bestLapText;
    public TMP_Text avgLapText;
    public TMP_Text totalRaceText;
    public TMP_Text collisionsText;
    public TMP_Text maxSpeedText;
    public TMP_Text avgSpeedText;
    public TMP_Text consistencyText;
    public TMP_Text scoreText;
    public Button resultsOkayButton;

    // Active session settings
    private int playerID;
    public DifficultyLevel currentDifficulty { get; private set; }
    private bool gameReady = false;

    // Session tracking
    private SessionData sessionData;
    private float sessionStartTime;
    private bool sessionActive = false;
    private bool sessionEnded = false;
    private bool raceStarted = false;

    // Racing metrics
    private float totalDuration;
    private float avgSpeed;
    private float bestLapTime;
    private float avgLapTime;
    private float consistency;
    private int currentLap = 0;
    private List<float> lapTimes = new List<float>();
    private float currentLapStartTime;
    private int collisionCount = 0;
    private List<float> speedSamples = new List<float>();
    private float maxSpeedReached = 0f;

    // Countdown
    private float countdownTimer;
    internal bool isCountingDown;

    void Start()
    {
        // Load settings from MainMenuManager or use fallback
        LoadSessionSettings();
        
        if (checkpoint == null)
        {
            Debug.LogError("No checkpoint assigned! Please add checkpoint GameObject to the reference.");
            return;
        }

        // Verify Checkpoint component exists
        Checkpoint checkpointComponent = checkpoint.GetComponent<Checkpoint>();
        if (checkpointComponent == null)
        {
            Debug.LogError($"Checkpoint ({checkpoint.name}) is missing Checkpoint component!");
        }
        else
        {
            Debug.Log($"Checkpoint found: {checkpoint.name}");
        }

        if (controlsOkayButton != null)
            controlsOkayButton.onClick.AddListener(OnControlsOkayClicked);
        
        if (resultsOkayButton != null)
            resultsOkayButton.onClick.AddListener(OnResultsOkayClicked);

        ShowControlsPanel();

        Debug.Log($"Racing Game Session initialized - Player ID: {playerID}, Difficulty: {currentDifficulty}, Laps: {totalLaps}");
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
            controlsText.text = @"<size=50><b>RACING GAME CONTROLS</b></size>

<b>MOVEMENT:</b>
• W / Up Arrow - Move Forward
• S / Down Arrow - Brake
• A / Left Arrow - Turn Left
• D / Right Arrow - Turn Right

<b>OBJECTIVE:</b>
Race around the tracks and complete 3 laps as fast as possible!
Avoid colliding with the walls or obstacles.

<b>TIP:</b>
Slow down when approaching the obstacles to maneuvre around them!";
        }
        
        SetGamePaused(true);
    }

    void OnControlsOkayClicked()
    {
        Debug.Log("Control button clicked");
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
        isCountingDown = true;
        SetGamePaused(true);
    }

    void Update()
    {
        // Handle countdown
        if (isCountingDown)
        {
            UpdateCountdown();
            return;
        }

        // Sample speed periodically
        if (sessionActive && Time.frameCount % 30 == 0) // Every ~0.5 seconds at 60 FPS
        {
            SampleSpeed();
        }

        // Allow player to quit race with ESC key
        if (sessionActive && !sessionEnded && Input.GetKeyDown(KeyCode.Escape))
        {
            OnApplicationQuit();
            QuitRace();
        }
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
            else if (!gameReady)
            {
                countdownText.text = "GO!";
                countdownText.color = Color.green;
                gameReady = true;
                isCountingDown = false;
                Invoke(nameof(StartRace), 1f);
            }
        }
    }

    void StartRace()
    {
        if (raceStarted) return;

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        SetGamePaused(false);

        raceStarted = true;
        sessionActive = true;
        sessionStartTime = Time.time;
        currentLapStartTime = Time.time;
        sessionData = new SessionData("Racing");
        sessionData.playerID = playerID;
        sessionData.difficultyLevel = currentDifficulty;

        currentLap = 1;

        Debug.Log("GO! Race STARTED!");
    }

    void SampleSpeed()
    {
        if (racecar == null) return;

        RaceCarController controller = racecar.GetComponent<RaceCarController>();
        if (controller != null)
        {
            float speed = controller.currentSpeed;
            
            speedSamples.Add(speed);

            if (speed > maxSpeedReached)
            {
                maxSpeedReached = speed;
            }
        }
    }

    // Called by Checkpoint when player passes through
    public void OnLapCompleted()
    {
        if (!sessionActive || sessionEnded) return;

        float lapTime = Time.time - currentLapStartTime;
        lapTimes.Add(lapTime);
        
        Debug.Log($"Lap {currentLap}/{totalLaps} completed in {lapTime:F2} seconds!");

        currentLap++;
        currentLapStartTime = Time.time;

        // Check if race is finished
        if (currentLap > totalLaps)
        {
            EndRace(true);
        }
    }

    // Method to handle collision with full Collision data (called by RaceCarController)
    public void OnCollision(Collision collision)
    {
        if (!sessionActive || sessionEnded) return;

        // Ignore collisions with ground/track (check by layer or name patterns)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
            collision.gameObject.layer == LayerMask.NameToLayer("Track") ||
            collision.gameObject.name.ToLower().Contains("ground") ||
            collision.gameObject.name.ToLower().Contains("track") ||
            collision.gameObject.name.ToLower().Contains("floor") ||
            collision.gameObject.name.ToLower().Contains("checkpoint"))
        {
            return;
        }

        // Count ALL other collisions as obstacles
        collisionCount++;
        Debug.Log($"Collision with {collision.gameObject.name}! Total: {collisionCount}");
    }

    /// <summary>
    /// Call this when player quits race early (ESC key, quit button, etc.)
    /// </summary>
    public void QuitRace()
    {
        if (!sessionActive || sessionEnded) return;
        
        Debug.Log("Player quit race early - saving partial data");
        EndRace(false); // Mark as not completed
    }

    void EndRace(bool completed)
    {
        if (!sessionActive || sessionEnded) return;

        sessionEnded = true;
        sessionActive = false;

        totalDuration = Time.time - sessionStartTime;
        avgSpeed = speedSamples.Count > 0 ? CalculateAverage(speedSamples) : 0f;
        bestLapTime = lapTimes.Count > 0 ? Mathf.Min(lapTimes.ToArray()) : 0f;
        avgLapTime = lapTimes.Count > 0 ? CalculateAverage(lapTimes) : 0f;
        consistency = CalculateConsistency(lapTimes);

        // Populate session data
        sessionData.sessionDuration = totalDuration;
        sessionData.score = CalculateScore(completed, bestLapTime, collisionCount);
        sessionData.deaths = completed ? 0 : 1;
        sessionData.completed = completed;

        // Add racing-specific data
        sessionData.gameSpecificData["lapsCompleted"] = currentLap - 1;
        sessionData.gameSpecificData["bestLapTime"] = bestLapTime;
        sessionData.gameSpecificData["avgLapTime"] = avgLapTime;
        sessionData.gameSpecificData["totalRaceTime"] = totalDuration;
        sessionData.gameSpecificData["collisions"] = collisionCount;
        sessionData.gameSpecificData["maxSpeed"] = maxSpeedReached;
        sessionData.gameSpecificData["avgSpeed"] = avgSpeed;
        sessionData.gameSpecificData["consistency"] = consistency;

        // Save to CSV
        GameplayDataCollector.Instance.SaveSessionData(sessionData);

        Debug.Log($"Race ENDED - Completed: {completed} | Laps: {currentLap - 1}/{totalLaps} | Best Lap: {bestLapTime:F2}s");

        ShowResults(completed);
    }

    void ShowResults(bool completed)
    {
        HideAllPanels();
        
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
        }

        if (resultsTitleText != null)
        {
            resultsTitleText.text = completed ? "COMPLETED!" : "DNF";
            resultsTitleText.color = completed ? Color.green : Color.red;
        }

        if (lapsCompletedText != null)
            lapsCompletedText.text = $"Laps Completed: {currentLap - 1:0.00}";

        if (bestLapText != null)
            bestLapText.text = $"Best Lap: {bestLapTime:0.00}";

        if (avgLapText != null)
            avgLapText.text = $"Avg Lap: {avgLapTime:0.00}";

        if (totalRaceText != null)
            totalRaceText.text = $"Duration: {totalDuration:0.00}";

        if (collisionsText != null)
            collisionsText.text = $"Collisions: {collisionCount}";

        if (maxSpeedText != null)
            maxSpeedText.text = $"Max speed: {maxSpeedReached:0.00}";

        if (avgSpeedText != null)
        avgSpeedText.text = $"Avg Speed: {avgSpeed:0.00}";

        if (consistencyText != null)
        consistencyText.text = $"Consistency: {consistency:0.00}";

        if (scoreText != null)
            scoreText.text = $"Score: {sessionData.score}";

        SetGamePaused(true);
    }

    void OnResultsOkayClicked()
    {
        Debug.Log("Returning to main menu...");
        Time.timeScale = 1f; // Ensure time is unpaused before loading scene
        SceneManager.LoadScene("MainMenu");
    }

    int CalculateScore(bool completed, float bestLapTime, int collisions)
    {
        if (!completed) return 0;

        int baseScore = 1000;
        int speedBonus = bestLapTime < 20f ? 500 : (bestLapTime < 30f ? 300 : 100);
        int collisionPenalty = collisions * 50;

        return Mathf.Max(0, baseScore + speedBonus - collisionPenalty);
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

    float CalculateConsistency(List<float> lapTimes)
    {
        if (lapTimes.Count < 2) return 1f;

        float avg = CalculateAverage(lapTimes);
        float variance = 0f;

        foreach (float time in lapTimes)
        {
            variance += Mathf.Pow(time - avg, 2);
        }

        float stdDev = Mathf.Sqrt(variance / lapTimes.Count);
        
        // Lower standard deviation = higher consistency
        // Normalize to 0-1 scale (0 = inconsistent, 1 = very consistent)
        return Mathf.Clamp01(1f - (stdDev / avg));
    }

    // Auto-save on scene change or application quit
    void OnDestroy()
    {
        // If session was active but never ended, save the data
        if (sessionActive && !sessionEnded)
        {
            Debug.LogWarning("RacingGameSession destroyed before EndRace was called - saving partial data");
            EndRace(false); // Mark as incomplete
        }
    }

    void OnApplicationQuit()
    {
        // Same logic for when application quits
        if (sessionActive && !sessionEnded)
        {
            Debug.LogWarning("Application quitting - saving partial race data");
            EndRace(false);
        }
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
        if (racecar != null)
        {
            RaceCarController carController = racecar.GetComponent<RaceCarController>();
            if (carController != null) carController.enabled = !paused;
        }
    }
}