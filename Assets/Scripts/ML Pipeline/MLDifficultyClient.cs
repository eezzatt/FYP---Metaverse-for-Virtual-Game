using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// MLDifficultyClient
/// ──────────────────
/// Attach this to the same GameObject as FightingGameSession or RacingGameSession.
///
/// After a session ends, call RequestPrediction(sessionData) from within
/// ShowResults() in either session script. The client posts gameplay metrics
/// to the Flask server and populates the XAI panel on the results screen.
///
/// UI wiring (set in Inspector):
///   xaiPanel              – parent panel to show/hide
///   recommendationText    – e.g.  "Recommended: Hard"
///   confidenceText        – e.g.  "Confidence: 87%"
///   explanationText       – plain-language sentence from server
///   feature1Name/Value    – top feature row 1
///   feature2Name/Value    – top feature row 2
///   feature3Name/Value    – top feature row 3
///   loadingIndicator      – spinner or "Analysing…" text (optional)
/// </summary>
public class MLDifficultyClient : MonoBehaviour
{
    // ── Server ──────────────────────────────────────────────────────────────
    [Header("Server Settings")]
    [Tooltip("URL of the Flask prediction server")]
    public string serverUrl = "http://localhost:5000/predict";

    [Tooltip("Seconds before giving up on the request")]
    public float requestTimeout = 10f;

    // ── XAI Panel UI ────────────────────────────────────────────────────────
    [Header("XAI Panel UI")]
    public GameObject xaiPanel;
    public TMP_Text recommendationText;
    public TMP_Text confidenceText;
    public TMP_Text explanationText;

    [Header("Top Feature Rows")]
    public TMP_Text feature1Name;
    public TMP_Text feature1Value;
    public TMP_Text feature2Name;
    public TMP_Text feature2Value;
    public TMP_Text feature3Name;
    public TMP_Text feature3Value;

    [Header("Optional")]
    public GameObject loadingIndicator;

    [Header("Next Round Button")]
    public Button nextRoundButton;
    private DifficultyLevel recommendedDifficulty = DifficultyLevel.Medium;

    // ── Colour Coding ───────────────────────────────────────────────────────
    private static readonly Color ColourTooEasy  = new Color(0.2f, 0.8f, 0.2f);   // green
    private static readonly Color ColourBalanced = new Color(0.2f, 0.6f, 1.0f);   // blue
    private static readonly Color ColourTooHard  = new Color(1.0f, 0.4f, 0.2f);   // orange-red

    // ────────────────────────────────────────────────────────────────────────
    // Public entry point – call this from ShowResults() in your session scripts
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call at the end of a session to fetch a difficulty recommendation.
    /// Pass in the completed SessionData object (after all metrics are set).
    /// </summary>

    public void Start()
    {
        if (nextRoundButton != null)
            nextRoundButton.onClick.AddListener(OnNextRoundClicked);
    }

    public void RequestPrediction(SessionData session)
    {
        if (xaiPanel != null) xaiPanel.SetActive(false);
        if (loadingIndicator != null) loadingIndicator.SetActive(true);

        PredictRequestBody body = BuildRequestBody(session);

        if (body == null)
        {
            Debug.LogWarning("[MLClient] Could not build request body – unknown game type.");
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            return;
        }

        StartCoroutine(PostPrediction(body));
    }

    // ────────────────────────────────────────────────────────────────────────
    // Build the JSON body that the Flask server expects
    // ────────────────────────────────────────────────────────────────────────

    private PredictRequestBody BuildRequestBody(SessionData session)
    {
        var body = new PredictRequestBody { gameType = session.gameType };
        body.sessionDuration = session.sessionDuration;

        var d = session.gameSpecificData;

        if (session.gameType == "Fighting")
        {
            body.hitsDealt       = GetFloat(d, "hitsDealt");
            body.hitsTaken       = GetFloat(d, "hitsTaken");
            body.combosExecuted  = GetFloat(d, "combosExecuted");
            body.perfectDodges   = GetFloat(d, "perfectDodges");
            body.playerAccuracy  = GetFloat(d, "playerAccuracy");
            body.avgReactionTime = GetFloat(d, "avgReactionTime");
            body.victory         = GetFloat(d, "victory");
        }
        else if (session.gameType == "Racing")
        {
            body.lapsCompleted = GetFloat(d, "lapsCompleted");
            body.bestLapTime   = GetFloat(d, "bestLapTime");
            body.avgLapTime    = GetFloat(d, "avgLapTime");
            body.totalRaceTime = GetFloat(d, "totalRaceTime");
            body.collisions    = GetFloat(d, "collisions");
            body.maxSpeed      = GetFloat(d, "maxSpeed");
            body.avgSpeed      = GetFloat(d, "avgSpeed");
            body.consistency   = GetFloat(d, "consistency");
            body.completed     = GetFloat(d, "completed");
        }
        else
        {
            return null;
        }

        return body;
    }

    // ────────────────────────────────────────────────────────────────────────
    // HTTP POST coroutine
    // ────────────────────────────────────────────────────────────────────────

    private IEnumerator PostPrediction(PredictRequestBody body)
    {
        string json = JsonUtility.ToJson(body);
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

        using UnityWebRequest www = new UnityWebRequest(serverUrl, "POST");
        www.uploadHandler   = new UploadHandlerRaw(jsonBytes);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.timeout = Mathf.RoundToInt(requestTimeout);

        yield return www.SendWebRequest();

        if (loadingIndicator != null) loadingIndicator.SetActive(false);

        if (www.result == UnityWebRequest.Result.Success)
        {
            HandleSuccess(www.downloadHandler.text);
        }
        else
        {
            HandleError(www.error, www.downloadHandler.text);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Response handling
    // ────────────────────────────────────────────────────────────────────────

    private void HandleSuccess(string jsonResponse)
    {
        Debug.Log($"[MLClient] Response: {jsonResponse}");

        PredictResponse response;
        try
        {
            response = JsonUtility.FromJson<PredictResponse>(jsonResponse);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MLClient] JSON parse error: {ex.Message}");
            ShowFallback("Could not parse server response.");
            return;
        }

        PopulateXAIPanel(response);
    }

    private void HandleError(string error, string body)
    {
        Debug.LogError($"[MLClient] Request failed: {error}\nBody: {body}");
        ShowFallback("ML server unavailable. Play on and the system will adapt.");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Populate UI
    // ────────────────────────────────────────────────────────────────────────

    private void PopulateXAIPanel(PredictResponse r)
    {
        if (xaiPanel != null) xaiPanel.SetActive(true);

        // Recommendation label
        string label = r.prediction switch
        {
            "TooEasy"  => "▲  Try a Harder Difficulty",
            "TooHard"  => "▼  Try an Easier Difficulty",
            _          => "✓  Current Difficulty is Just Right",
        };

        Color labelColour = r.prediction switch
        {
            "TooEasy"  => ColourTooEasy,
            "TooHard"  => ColourTooHard,
            _          => ColourBalanced,
        };

        recommendedDifficulty = r.prediction switch
        {
            "TooEasy" => DifficultyLevel.Hard,
            "TooHard" => DifficultyLevel.Easy,
            _         => DifficultyLevel.Medium,
        };

        if (recommendationText != null)
        {
            recommendationText.text  = label;
            recommendationText.color = labelColour;
        }

        // Confidence
        if (confidenceText != null)
            confidenceText.text = $"Confidence: {r.confidence * 100f:F0}%";

        // Plain-language explanation
        if (explanationText != null)
            explanationText.text = r.explanation;

        // Top 3 features
        PopulateFeatureRow(feature1Name, feature1Value, r.topFeatures, 0);
        PopulateFeatureRow(feature2Name, feature2Value, r.topFeatures, 1);
        PopulateFeatureRow(feature3Name, feature3Value, r.topFeatures, 2);
    }

    private void OnNextRoundClicked()
    {
        MainMenuManager.SetDifficulty(recommendedDifficulty);
        MainMenuManager.SetIsNextRound(true);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void PopulateFeatureRow(TMP_Text nameField, TMP_Text valueField,
                                    FeatureInfo[] features, int index)
    {
        if (features == null || index >= features.Length) return;

        var f = features[index];

        if (nameField  != null) nameField.text  = f.displayName;
        if (valueField != null) valueField.text  = f.value.ToString("F2");
    }

    private void ShowFallback(string message)
    {
        if (xaiPanel != null) xaiPanel.SetActive(true);

        if (recommendationText != null)
        {
            recommendationText.text  = "Recommendation unavailable";
            recommendationText.color = Color.grey;
        }

        if (explanationText != null)
            explanationText.text = message;

        if (confidenceText != null)
            confidenceText.text = "";
    }

    // ────────────────────────────────────────────────────────────────────────
    // Utility
    // ────────────────────────────────────────────────────────────────────────

    private float GetFloat(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out object val)) return 0f;
        try { return Convert.ToSingle(val); }
        catch { return 0f; }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Serialisable data models
    // ────────────────────────────────────────────────────────────────────────

    [Serializable]
    private class PredictRequestBody
    {
        // Common
        public string gameType;
        public float  sessionDuration;

        // Fighting
        public float hitsDealt;
        public float hitsTaken;
        public float combosExecuted;
        public float perfectDodges;
        public float playerAccuracy;
        public float avgReactionTime;
        public float victory;

        // Racing
        public float lapsCompleted;
        public float bestLapTime;
        public float avgLapTime;
        public float totalRaceTime;
        public float collisions;
        public float maxSpeed;
        public float avgSpeed;
        public float consistency;
        public float completed;
    }

    [Serializable]
    private class PredictResponse
    {
        public string      prediction;
        public float       confidence;
        public string      explanation;
        public FeatureInfo[] topFeatures;
    }

    [Serializable]
    private class FeatureInfo
    {
        public string name;
        public string displayName;
        public float  importance;
        public float  value;
    }
}