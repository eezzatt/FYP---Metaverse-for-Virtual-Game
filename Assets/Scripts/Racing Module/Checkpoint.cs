using UnityEngine;

/// <summary>
/// Checkpoint trigger for racing game
/// Every pass through this checkpoint counts as completing a lap
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Visual Feedback")]
    public Color normalColor = Color.yellow;
    public Color passedColor = Color.green;
    
    private Renderer checkpointRenderer;

    void Start()
    {
        checkpointRenderer = GetComponent<Renderer>();
        if (checkpointRenderer != null)
        {
            checkpointRenderer.material.color = normalColor;
        }
        
        // Make sure collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it's the player's car
        if (other.CompareTag("Player"))
        {
            OnCheckpointPassed(other.gameObject);
        }
    }

    void OnCheckpointPassed(GameObject player)
    {
        RacingGameSession session = FindFirstObjectByType<RacingGameSession>();
        
        if (session != null)
        {
            // Every checkpoint pass counts as a lap completion
            session.OnLapCompleted();
            
            // Visual feedback
            MarkAsPassed();
            
            // Reset visual after a short delay
            Invoke("ResetCheckpoint", 0.5f);
        }
    }

    void MarkAsPassed()
    {
        if (checkpointRenderer != null)
        {
            checkpointRenderer.material.color = passedColor;
        }
    }

    // Public method to reset checkpoint visual
    public void ResetCheckpoint()
    {
        if (checkpointRenderer != null)
        {
            checkpointRenderer.material.color = normalColor;
        }
    }

    // Visualize checkpoint in editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}