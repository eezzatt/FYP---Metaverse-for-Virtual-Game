using UnityEngine;
using System.Collections.Generic;

public class RacingGameSession : MonoBehaviour
{
    [Header("Session Settings")]
    public int playerID;
    public DifficultyLevel currentDifficulty;
    public int totalLaps;

    [Header("References")]
    public GameObject racecar;
    public GameObject checkpoint; // Single checkpoint reference

    // Session tracking
    private SessionData sessionData;
    private float sessionStartTime;
    private bool sessionActive = false;
    private bool sessionEnded = false;
    private bool raceStarted = false;

    // Racing metrics
    private int currentLap = 0;
    private List<float> lapTimes = new List<float>();
    private float currentLapStartTime;
    private int collisionCount = 0;
    private List<float> speedSamples = new List<float>();
    private float maxSpeedReached = 0f;

    // Countdown
    private float countdownTimer = 3f;
    internal bool isCountingDown;

    void Start()
    {
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

        StartCountdown();
    }

    void StartCountdown()
    {
        isCountingDown = true;
        Debug.Log("Race starting in 3...");
    }

    void Update()
    {
        // Handle countdown
        if (isCountingDown)
        {
            countdownTimer -= Time.deltaTime;
            
            if (countdownTimer <= 2f && countdownTimer > 1f)
            {
                Debug.Log("2...");
            }
            else if (countdownTimer <= 1f && countdownTimer > 0f)
            {
                Debug.Log("1...");
            }
            else if (countdownTimer <= 0f)
            {
                isCountingDown = false;
                StartRace();
            }
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
            QuitRace();
        }
    }

    void StartRace()
    {
        if (raceStarted) return;

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
                // Debug.Log("New max speed reached: " + maxSpeedReached);
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

        // foreach(float speed in speedSamples)
        // {
        //     Debug.Log(speed);
        // }
        
        sessionEnded = true;
        sessionActive = false;

        float totalDuration = Time.time - sessionStartTime;
        float avgSpeed = speedSamples.Count > 0 ? CalculateAverage(speedSamples) : 0f;
        float bestLapTime = lapTimes.Count > 0 ? Mathf.Min(lapTimes.ToArray()) : 0f;
        float avgLapTime = lapTimes.Count > 0 ? CalculateAverage(lapTimes) : 0f;
        float consistency = CalculateConsistency(lapTimes);

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
        // Debug.Log(sum);
        // Debug.Log(values.Count);
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
}