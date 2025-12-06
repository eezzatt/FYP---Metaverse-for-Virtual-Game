using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// UI manager for displaying session results and optional player feedback
/// </summary>
public class SessionResultsUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject resultsPanel;
    public GameObject feedbackPanel;
    
    [Header("Results Display")]
    public TMP_Text titleText;
    public TMP_Text durationText;
    public TMP_Text scoreText;
    public TMP_Text accuracyText;
    public TMP_Text performanceText;
    
    [Header("Feedback Survey (Optional)")]
    public TMP_Text difficultyQuestionText;
    public Button tooEasyButton;
    public Button justRightButton;
    public Button tooHardButton;
    
    [Header("Navigation")]
    public Button continueButton;
    public Button retryButton;
    public Button mainMenuButton;
    
    private string playerFeedback = "";

    void Start()
    {
        // Initially hide panels
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        
        // Setup button listeners
        if (tooEasyButton != null)
            tooEasyButton.onClick.AddListener(() => OnFeedbackSelected("TooEasy"));
        
        if (justRightButton != null)
            justRightButton.onClick.AddListener(() => OnFeedbackSelected("JustRight"));
        
        if (tooHardButton != null)
            tooHardButton.onClick.AddListener(() => OnFeedbackSelected("TooHard"));
        
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinue);
        
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenu);
    }

    /// <summary>
    /// Display results for fighting game
    /// </summary>
    public void ShowFightingResults(bool victory, float duration, float damageTaken, 
                                     float damageDealt, float accuracy)
    {
        resultsPanel.SetActive(true);
        
        // Set title based on victory
        titleText.text = victory ? "VICTORY!" : "DEFEAT";
        titleText.color = victory ? Color.green : Color.red;
        
        // Display stats
        durationText.text = $"Duration: {duration:F1}s";
        scoreText.text = $"Damage Dealt: {damageDealt:F0}";
        accuracyText.text = $"Accuracy: {accuracy:P0}";
        performanceText.text = $"Damage Taken: {damageTaken:F0}";
        
        // Optionally show feedback survey after a delay
        Invoke(nameof(ShowFeedbackSurvey), 2f);
    }

    /// <summary>
    /// Display results for racing game
    /// </summary>
    public void ShowRacingResults(bool completed, float totalTime, float bestLap, 
                                  float avgLap, int collisions)
    {
        resultsPanel.SetActive(true);
        
        titleText.text = completed ? "RACE COMPLETE!" : "DNF";
        titleText.color = completed ? Color.green : Color.red;
        
        durationText.text = $"Total Time: {totalTime:F2}s";
        scoreText.text = $"Best Lap: {bestLap:F2}s";
        accuracyText.text = $"Average Lap: {avgLap:F2}s";
        performanceText.text = $"Collisions: {collisions}";
        
        Invoke(nameof(ShowFeedbackSurvey), 2f);
    }

    void ShowFeedbackSurvey()
    {
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(true);
            difficultyQuestionText.text = "How was the difficulty?";
        }
    }

    void OnFeedbackSelected(string feedback)
    {
        playerFeedback = feedback;
        Debug.Log($"Player feedback: {feedback}");
        
        // TODO: You can log this to your data collector
        // This adds another dimension to your data:
        // - If player says "TooEasy" on Medium → Maybe increase difficulty
        // - If player says "TooHard" on Medium → Maybe decrease difficulty
        
        // Hide feedback panel after selection
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }
        
        // Highlight the selected button briefly
        Button selectedButton = feedback switch
        {
            "TooEasy" => tooEasyButton,
            "TooHard" => tooHardButton,
            _ => justRightButton
        };
        
        if (selectedButton != null)
        {
            ColorBlock colors = selectedButton.colors;
            colors.normalColor = Color.green;
            selectedButton.colors = colors;
        }
    }

    void OnContinue()
    {
        // Continue to next challenge or mini-game
        Debug.Log("Continue to next game");
        resultsPanel.SetActive(false);
        // You would load the next scene or reset the game here
    }

    void OnRetry()
    {
        // Retry the same game
        Debug.Log("Retrying game");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnMainMenu()
    {
        // Return to main menu
        Debug.Log("Return to main menu");
        SceneManager.LoadScene("MainMenu"); // Adjust scene name as needed
    }

    /// <summary>
    /// Get the player's difficulty feedback for data analysis
    /// </summary>
    public string GetPlayerFeedback()
    {
        return playerFeedback;
    }
}