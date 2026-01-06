using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;

/// <summary>
/// Manages obstacle spawning based on difficulty level
/// Easy: No obstacles
/// Medium: Obstacles only on straight sections
/// Hard: Obstacles on both straight and curved sections
/// </summary>
public class ObstacleManager : MonoBehaviour
{    
    [Header("Obstacle Prefab")]
    public GameObject obstaclePrefab; // Assign the obstacle prefab in inspector
    
     [Tooltip("Number of obstacles per curved section (Hard difficulty only)")]
    public int obstaclesPerCurve = 2;
    
    [Header("Spawn Settings")]
    [Tooltip("Minimum distance from track edges")]
    public float edgeBuffer = 2f;
    
    [Tooltip("Track width for obstacle placement")]
    public float trackWidth = 10f;
    
    [Tooltip("Height above track surface")]
    public float obstacleHeight = 0.5f;

    [Header("Arch Turn Angles")]
    [Tooltip("Turn angle in degrees for each arch (90 = right turn, 180 = U-turn)")]
    public List<float> archCircumferences = new List<float>
    {
        180f, 180f, 180f, 90f, 180f, 90f
    };

    private int obstaclesPerStraight;
    private RacingGameSession gameSession;
    private DifficultyLevel currentDifficulty;
    // Track section references
    private List<GameObject> straightSections = new List<GameObject>();
    private List<GameObject> curvedSections = new List<GameObject>();   
    private List<GameObject> spawnedObstacles = new List<GameObject>();

    void Start()
    {
        gameSession = FindFirstObjectByType<RacingGameSession>();
        currentDifficulty = gameSession.currentDifficulty;
        FindTrackSections();
        SpawnObstaclesForDifficulty();
    }

    /// <summary>
    /// Find all track sections in the scene by searching children of "Track" parent
    /// </summary>
    void FindTrackSections()
    {
        // Find the parent Track object
        GameObject trackParent = GameObject.Find("Track");
        
        if (trackParent == null)
        {
            Debug.LogError("ObstacleManager: 'Track' parent object not found!");
            return;
        }

        // Search through all children
        foreach (Transform child in trackParent.transform)
        {
            if (child.name.Contains("TrackStraight"))
            {
                straightSections.Add(child.gameObject);
            }
            else if (child.name.Contains("TrackArch"))
            {
                curvedSections.Add(child.gameObject);
            }
        }

        Debug.Log($"Found {straightSections.Count} straight sections and {curvedSections.Count} curved sections");
    }

    /// <summary>
    /// Spawn obstacles based on current difficulty level
    /// </summary>
    void SpawnObstaclesForDifficulty()
    {
        ClearExistingObstacles();

        if (obstaclePrefab == null)
        {
            Debug.LogWarning("ObstacleManager: No obstacle prefab assigned! Creating default obstacles.");
            obstaclePrefab = CreateDefaultObstaclePrefab();
        }

        switch (currentDifficulty)
        {
            case DifficultyLevel.Easy:
                // No obstacles
                Debug.Log("Easy difficulty - No obstacles spawned");
                break;

            case DifficultyLevel.Medium:
                // Only straight sections
                SpawnOnStraightSections();
                Debug.Log($"Medium difficulty - Spawned obstacles on {straightSections.Count} straight sections");
                break;

            case DifficultyLevel.Hard:
                // All sections
                SpawnOnStraightSections();
                SpawnOnCurvedSections();
                Debug.Log($"Hard difficulty - Spawned obstacles on all sections");
                break;
        }
    }

    /// <summary>
    /// Spawn obstacles on straight track sections
    /// </summary>
    void SpawnOnStraightSections()
    {
        foreach (GameObject section in straightSections)
        {
            Vector3 sectionPos = section.transform.position;
            Bounds bounds = GetSectionBounds(section);
            
            // Determine if section is aligned with X or Z axis
            bool isXAligned = Mathf.Abs(section.transform.rotation.eulerAngles.y) < 45f || 
                            Mathf.Abs(section.transform.rotation.eulerAngles.y - 180f) < 45f;

            if (isXAligned)
            {
                SetNumberOfObstacles(bounds.extents.z);
            }
            else
            {
                SetNumberOfObstacles(bounds.extents.x);
            }
            
            // Spawn multiple obstacles along the section
            for (int i = 0; i < obstaclesPerStraight; i++)
            {
                Vector3 spawnPos = GetRandomPositionInStraight(section, bounds, isXAligned);
                SpawnObstacle(spawnPos, section.transform.rotation);
            }
        }
    }

    /// <summary>
    /// Spawn obstacles on curved track sections (Hard difficulty only)
    /// </summary>
    void SpawnOnCurvedSections()
    {
        int j = 0;
        foreach (GameObject section in curvedSections)
        {
            Vector3 sectionPos = section.transform.position;
            Bounds bounds = GetSectionBounds(section);
            float archCircumference = archCircumferences[j];
            Debug.Log(bounds.extents.x + " " + bounds.extents.y + " " + bounds.extents.z);
            
            // Spawn fewer obstacles on curves to avoid blocking the path
            for (int i = 0; i < obstaclesPerCurve; i++)
            {
                Vector3 spawnPos = GetRandomPositionInCurve(section, bounds, archCircumference);
                SpawnObstacle(spawnPos, section.transform.rotation);
            }
            j++;
        }
    }

    /// <summary>
    /// Get random position within a straight section
    /// </summary>
    Vector3 GetRandomPositionInStraight(GameObject section, Bounds bounds, bool isXAligned)
    {
        Vector3 pos = section.transform.position;
        
        if (isXAligned)
        {
            // Section runs along X axis
            float zOffset = Random.Range(-bounds.extents.z * 0.5f, bounds.extents.z * 0.5f);
            float xOffset = Random.Range(-trackWidth/2 + edgeBuffer, trackWidth/2 - edgeBuffer);
            pos += new Vector3(xOffset, obstacleHeight, zOffset);
        }
        else
        {
            // Section runs along Z axis
            float zOffset = Random.Range(-trackWidth/2 + edgeBuffer, trackWidth/2 - edgeBuffer);
            float xOffset = Random.Range(-bounds.extents.x * 0.5f, bounds.extents.x * 0.5f);
            pos += new Vector3(xOffset, obstacleHeight, zOffset);
        }
        
        return pos;
    }

    /// <summary>
    /// Get random position within a curved section
    /// </summary>
    Vector3 GetRandomPositionInCurve(GameObject section, Bounds bounds, float archCircumference)
    {
        float xOffset;
        float zOffset;
        Vector3 pos = section.transform.position;
        float y_rotation = section.transform.eulerAngles.y;
        if (archCircumference == 90)
        {
            switch (y_rotation)
            {
                case 0:
                    xOffset = Random.Range(bounds.extents.x * 0.2f, bounds.extents.x * 0.4f);
                    zOffset = Random.Range(bounds.extents.z * 0.2f, bounds.extents.z * 0.4f);
                    break;
                
                case 90:
                    xOffset = Random.Range(bounds.extents.x * 0.2f, bounds.extents.x * 0.4f);
                    zOffset = Random.Range(-bounds.extents.z * 0.4f, -bounds.extents.z * 0.4f);
                    break;

                case 180:  
                    xOffset = Random.Range(-bounds.extents.x * 0.2f, -bounds.extents.x * 0.4f);
                    zOffset = Random.Range(-bounds.extents.z * 0.2f, -bounds.extents.z * 0.4f);
                    break;

                case 270:
                    xOffset = Random.Range(-bounds.extents.x * 0.2f, -bounds.extents.x * 0.4f);
                    zOffset = Random.Range(bounds.extents.z * 0.2f, bounds.extents.z * 0.4f);
                    break;
                
                default:
                    Debug.LogWarning($"Unexpected rotation: {y_rotation}. Using default offset.");
                    xOffset = Random.Range(bounds.extents.x * 0.2f, bounds.extents.x * 0.4f);
                    zOffset = Random.Range(bounds.extents.z * 0.2f, bounds.extents.z * 0.4f);
                    break;
            }
            
        }   
        else
        {
            switch (y_rotation)
            {
                case 0:
                    xOffset = Random.Range(-bounds.extents.x * 0.4f, bounds.extents.x * 0.4f);
                    zOffset = Random.Range(bounds.extents.z * 0.75f, bounds.extents.z * 0.79f);
                    break;
                
                case 90:
                    xOffset = Random.Range(bounds.extents.x * 0.75f, bounds.extents.x * 0.79f);
                    zOffset = Random.Range(-bounds.extents.z * 0.4f, bounds.extents.z * 0.4f);
                    break;

                case 180:
                    xOffset = Random.Range(-bounds.extents.x * 0.4f, bounds.extents.x * 0.4f);
                    zOffset = Random.Range(-bounds.extents.z * 0.75f, -bounds.extents.z * 0.79f);
                    break;

                case 270:
                    xOffset = Random.Range(-bounds.extents.x * 0.75f, -bounds.extents.x * 0.79f);
                    zOffset = Random.Range(-bounds.extents.z * 0.4f, bounds.extents.z * 0.4f);
                    break;

                default:
                    Debug.LogWarning($"Unexpected rotation: {y_rotation}. Using default offset.");
                    xOffset = Random.Range(bounds.extents.x * 0.4f, bounds.extents.x * 0.6f);
                    zOffset = Random.Range(bounds.extents.z * 0.4f, bounds.extents.z * 0.6f);
                    break;
            }
        }
        
        pos += new Vector3(xOffset, obstacleHeight, zOffset);
        
        return pos;
    }

    /// <summary>
    /// Get the bounds of a track section
    /// </summary>
    Bounds GetSectionBounds(GameObject section)
    {
        Renderer renderer = section.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        // Fallback: use default bounds
        return new Bounds(section.transform.position, new Vector3(20, 1, 20));
    }

    /// <summary>
    /// Spawn a single obstacle at the given position
    /// </summary>
    void SpawnObstacle(Vector3 position, Quaternion rotation)
    {
        GameObject obstacle = Instantiate(obstaclePrefab, position, rotation);
        obstacle.transform.parent = transform; // Parent to ObstacleManager for organization
        spawnedObstacles.Add(obstacle);
    }

    /// <summary>
    /// Clear all spawned obstacles
    /// </summary>
    void ClearExistingObstacles()
    {
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }
        spawnedObstacles.Clear();
    }

    /// <summary>
    /// Create a default obstacle prefab if none is assigned
    /// </summary>
    GameObject CreateDefaultObstaclePrefab()
    {
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = "DefaultObstacle";
        prefab.transform.localScale = new Vector3(2f, 1f, 1f);
        prefab.tag = "Obstacle";
        
        // Set red color
        Renderer renderer = prefab.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }
        
        // Make sure it has a collider
        Collider collider = prefab.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false; // Solid collision
        }
        
        return prefab;
    }

    /// <summary>
    /// Public method to change difficulty and respawn obstacles
    /// Call this from RacingGameSession or a difficulty selector
    /// </summary>
    public void SetDifficulty(DifficultyLevel newDifficulty)
    {
        currentDifficulty = newDifficulty;
        SpawnObstaclesForDifficulty();
    }

    public void SetNumberOfObstacles(float sectionLength)
    {
        if (sectionLength >= 50)
        {
            obstaclesPerStraight = 4;
        }
        else
        {
            obstaclesPerStraight = 3;
        }
    }

    // Visualize spawn areas in editor
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw straight sections in green
        Gizmos.color = Color.green;
        foreach (GameObject section in straightSections)
        {
            if (section != null)
            {
                Gizmos.DrawWireCube(section.transform.position, GetSectionBounds(section).size);
            }
        }

        // Draw curved sections in yellow
        Gizmos.color = Color.yellow;
        foreach (GameObject section in curvedSections)
        {
            if (section != null)
            {
                Gizmos.DrawWireCube(section.transform.position, GetSectionBounds(section).size);
            }
        }
    }
}