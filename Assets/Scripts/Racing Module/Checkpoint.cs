using UnityEngine;

/// <summary>
/// Checkpoint trigger for racing game
/// Place these along the track to track progress
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public int checkpointNumber = 0; // Order in the track (0 = start/finish)
    public bool isFinishLine = false;
    
    [Header("Visual Feedback")]
    public Color normalColor = Color.yellow;
    public Color passedColor = Color.green;
    
    private Renderer checkpointRenderer;
    private bool hasPassed = false;

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
        // FIXED: Use FindObjectOfType instead of FindFirstObjectByType
        RacingGameSession session = FindFirstObjectByType<RacingGameSession>();
        
        if (session != null)
        {
            if (isFinishLine)
            {
                // Player crossed finish line
                session.OnLapCompleted();
                ResetCheckpoint();
            }
            else
            {
                // Regular checkpoint
                if (!hasPassed)
                {
                    session.OnCheckpointPassed(checkpointNumber);
                    MarkAsPassed();
                }
            }
        }
    }

    void MarkAsPassed()
    {
        hasPassed = true;
        if (checkpointRenderer != null)
        {
            checkpointRenderer.material.color = passedColor;
        }
    }

    // ADDED: Public method to reset from RacingGameSession
    public void ResetCheckpoint()
    {
        hasPassed = false;
        if (checkpointRenderer != null)
        {
            checkpointRenderer.material.color = normalColor;
        }
    }

    // Visualize checkpoint in editor
    void OnDrawGizmos()
    {
        Gizmos.color = isFinishLine ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}