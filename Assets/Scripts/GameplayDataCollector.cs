using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class GameplayDataCollector : MonoBehaviour
{
    private static GameplayDataCollector instance;
    public static GameplayDataCollector Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameplayDataCollector>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameplayDataCollector");
                    instance = go.AddComponent<GameplayDataCollector>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private string csvFilePath;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCSV();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void InitializeCSV()
    {
        csvFilePath = Path.Combine(Application.dataPath, "GameplayData.csv");

        // Only create headers if file doesn't exist
        if (!File.Exists(csvFilePath))
        {
            // Write header row
            string headers = "SessionID,PlayerID,GameType,DifficultyLevel,SessionDuration,Score,Deaths,Completed,Timestamp";
            File.WriteAllText(csvFilePath, headers + "\n");
            Debug.Log($"Created new CSV file at: {csvFilePath}");
        }
        else
        {
            Debug.Log($"CSV file already exists at: {csvFilePath}");
        }
    }

    public void SaveSessionData(SessionData data)
    {
        // Ensure CSV exists
        if (!File.Exists(csvFilePath))
        {
            InitializeCSV();
        }

        // Build the row string
        List<string> values = new List<string>
        {
            data.sessionID,
            data.playerID,
            data.gameType,
            data.difficultyLevel.ToString(),
            data.sessionDuration.ToString("F2"),
            data.score.ToString(),
            data.deaths.ToString(),
            data.completed.ToString(),
            System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // Add game-specific data as additional columns
        if (data.gameSpecificData != null && data.gameSpecificData.Count > 0)
        {
            // Check if we need to add new headers for game-specific data
            string[] existingHeaders = File.ReadLines(csvFilePath).First().Split(',');
            List<string> newHeaders = new List<string>(existingHeaders);
            bool headersChanged = false;

            foreach (var key in data.gameSpecificData.Keys)
            {
                if (!newHeaders.Contains(key))
                {
                    newHeaders.Add(key);
                    headersChanged = true;
                }
            }

            // If headers changed, update the CSV file
            if (headersChanged)
            {
                UpdateCSVHeaders(newHeaders.ToArray());
            }

            // Now read the current headers to ensure proper column alignment
            string[] currentHeaders = File.ReadLines(csvFilePath).First().Split(',');
            
            // Add values in the correct order matching headers
            for (int i = values.Count; i < currentHeaders.Length; i++)
            {
                string header = currentHeaders[i];
                if (data.gameSpecificData.ContainsKey(header))
                {
                    values.Add(data.gameSpecificData[header].ToString());
                }
                else
                {
                    values.Add(""); // Empty value for missing data
                }
            }
        }

        // Write the row
        string row = string.Join(",", values);
        File.AppendAllText(csvFilePath, row + "\n");
        
        Debug.Log($"Session data saved: {data.sessionID} | {data.gameType} | Difficulty: {data.difficultyLevel}");
    }

    private void UpdateCSVHeaders(string[] newHeaders)
    {
        // Read all existing lines
        string[] allLines = File.ReadAllLines(csvFilePath);
        
        // Update the header line
        allLines[0] = string.Join(",", newHeaders);
        
        // Update all data rows to match new header count
        for (int i = 1; i < allLines.Length; i++)
        {
            string[] values = allLines[i].Split(',');
            
            // Pad with empty values if needed
            while (values.Length < newHeaders.Length)
            {
                System.Array.Resize(ref values, values.Length + 1);
                values[values.Length - 1] = "";
            }
            
            allLines[i] = string.Join(",", values);
        }
        
        // Write back to file
        File.WriteAllLines(csvFilePath, allLines);
        Debug.Log("CSV headers updated with new game-specific columns");
    }
}

[System.Serializable]
public class SessionData
{
    public string sessionID;
    public string playerID;
    public string gameType;
    public DifficultyLevel difficultyLevel;
    public float sessionDuration;
    public int score;
    public int deaths;
    public bool completed;
    public Dictionary<string, object> gameSpecificData;

    public SessionData(string gameType)
    {
        this.sessionID = System.Guid.NewGuid().ToString();
        this.playerID = "Player1"; // Can be changed to PlayerPrefs.GetString("PlayerID")
        this.gameType = gameType;
        this.gameSpecificData = new Dictionary<string, object>();
    }
}

public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard
}