using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Main Menu Manager - Handles game selection, difficulty selection, and player ID input
/// Attach this to an empty GameObject in your MainMenu scene
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Input field for player ID")]
    public TMP_InputField playerIDInput;
    
    [Tooltip("Dropdown for game selection")]
    public TMP_Dropdown gameSelectionDropdown;
    
    [Tooltip("Dropdown for difficulty selection")]
    public TMP_Dropdown difficultyDropdown;
    
    [Tooltip("Button to start the selected game")]
    public Button startGameButton;
    
    // [Tooltip("Button to quit application")]
    // public Button quitButton;
    
    [Header("Scene Names")]
    [Tooltip("Name of the fighting game scene")]
    public string fightingSceneName = "FightingGame";
    
    [Tooltip("Name of the racing game scene")]
    public string racingSceneName = "RacingGame";
    
    // [Header("Validation Feedback")]
    // [Tooltip("Text to display validation messages")]
    // public TMP_Text feedbackText;

    // Static variables to pass data between scenes
    public static int SelectedPlayerID { get; private set; }
    public static DifficultyLevel SelectedDifficulty { get; private set; }
    public static string SelectedGameType { get; private set; }

    void Start()
    {
        // Setup button listeners
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }
        
        // if (quitButton != null)
        // {
        //     quitButton.onClick.AddListener(OnQuitClicked);
        // }
        
        // Setup dropdowns
        SetupGameDropdown();
        SetupDifficultyDropdown();
        
        // Clear feedback text
        // if (feedbackText != null)
        // {
        //     feedbackText.text = "";
        // }
        
        Debug.Log("Main Menu loaded");
    }

    void SetupGameDropdown()
    {
        if (gameSelectionDropdown == null) return;
        
        gameSelectionDropdown.ClearOptions();
        gameSelectionDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            "Fighting Game",
            "Racing Game"
        });
        
        gameSelectionDropdown.value = 0;
    }

    void SetupDifficultyDropdown()
    {
        if (difficultyDropdown == null) return;
        
        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            "Easy",
            "Medium",
            "Hard"
        });
        
        difficultyDropdown.value = 0;
    }

    void OnStartGameClicked()
    {
        // Validate player ID
        if (!ValidatePlayerID())
        {
            ShowFeedback("Please enter a valid Player ID (numbers only)", Color.red);
            return;
        }
        
        // Get selected values
        int playerID = int.Parse(playerIDInput.text);
        string selectedGame = gameSelectionDropdown.options[gameSelectionDropdown.value].text;
        string selectedDifficulty = difficultyDropdown.options[difficultyDropdown.value].text;
        
        // Store values in static variables
        SelectedPlayerID = playerID;
        SelectedDifficulty = ParseDifficulty(selectedDifficulty);
        SelectedGameType = selectedGame;
        
        Debug.Log($"Starting game: {selectedGame} | Difficulty: {selectedDifficulty} | Player ID: {playerID}");
        
        // Load the appropriate scene
        LoadGameScene(selectedGame);
    }

    bool ValidatePlayerID()
    {
        if (playerIDInput == null) return false;
        
        string input = playerIDInput.text.Trim();
        
        // Check if empty
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }
        
        // Check if it's a valid number
        if (!int.TryParse(input, out int playerID))
        {
            return false;
        }
        
        // Check if it's a positive number
        if (playerID <= 0)
        {
            ShowFeedback("Player ID must be a positive number", Color.red);
            return false;
        }
        
        return true;
    }

    DifficultyLevel ParseDifficulty(string difficultyString)
    {
        switch (difficultyString)
        {
            case "Easy":
                return DifficultyLevel.Easy;
            case "Medium":
                return DifficultyLevel.Medium;
            case "Hard":
                return DifficultyLevel.Hard;
            default:
                return DifficultyLevel.Easy;
        }
    }

    void LoadGameScene(string gameType)
    {
        string sceneToLoad = "";
        
        if (gameType == "Fighting Game")
        {
            sceneToLoad = fightingSceneName;
        }
        else if (gameType == "Racing Game")
        {
            sceneToLoad = racingSceneName;
        }
        else
        {
            ShowFeedback("Invalid game selection", Color.red);
            return;
        }
        
        // Check if scene exists in build settings
        if (!SceneExists(sceneToLoad))
        {
            ShowFeedback($"Scene '{sceneToLoad}' not found in Build Settings!", Color.red);
            Debug.LogError($"Scene '{sceneToLoad}' is not added to Build Settings. Please add it in File > Build Settings");
            return;
        }
        
        ShowFeedback($"Loading {gameType}...", Color.green);
        
        // Load the scene
        SceneManager.LoadScene(sceneToLoad);
    }

    bool SceneExists(string sceneName)
    {
        // Check if scene is in build settings
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameInBuild == sceneName)
            {
                return true;
            }
        }
        
        return false;
    }

    void ShowFeedback(string message, Color color)
    {
        // if (feedbackText != null)
        // {
        //     feedbackText.text = message;
        //     feedbackText.color = color;
        // }
        
        Debug.Log(message);
    }

    void OnQuitClicked()
    {
        Debug.Log("Quitting application...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // Helper method to get the stored player ID (call this from other scripts)
    public static int GetPlayerID()
    {
        return SelectedPlayerID;
    }

    // Helper method to get the stored difficulty (call this from other scripts)
    public static DifficultyLevel GetDifficulty()
    {
        return SelectedDifficulty;
    }

    // Helper method to get the stored game type (call this from other scripts)
    public static string GetGameType()
    {
        return SelectedGameType;
    }
}