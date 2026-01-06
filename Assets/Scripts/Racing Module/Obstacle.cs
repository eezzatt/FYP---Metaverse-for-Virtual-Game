using UnityEngine;

/// <summary>
/// Simple obstacle component for racing game
/// Handles collision detection and visual feedback
/// </summary>
[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color normalColor = Color.red;
    public Color hitColor = Color.yellow;
    
    [Header("Physics Settings")]
    public float collisionForce = 5f;
    
    private Renderer obstacleRenderer;
    private Color originalColor;
    private bool wasHit = false;

    void Start()
    {
        // Ensure this object is tagged as Obstacle
        if (!gameObject.CompareTag("Obstacle"))
        {
            gameObject.tag = "Obstacle";
        }

        // Get renderer for visual feedback
        obstacleRenderer = GetComponent<Renderer>();
        if (obstacleRenderer != null)
        {
            obstacleRenderer.material.color = normalColor;
            originalColor = normalColor;
        }

        // Ensure collider is NOT a trigger (solid collision)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if player hit the obstacle
        if (collision.gameObject.CompareTag("Player"))
        {
            OnPlayerCollision(collision);
        }
    }

    void OnPlayerCollision(Collision collision)
    {
        if (wasHit) return; // Only trigger once per obstacle

        wasHit = true;
        RaceCarController car = FindFirstObjectByType<RaceCarController>();

        // Visual feedback
        if (obstacleRenderer != null)
        {
            obstacleRenderer.material.color = hitColor;
            car.currentSpeed -= 20f;
        }

        // Optional: Apply knockback force to player
        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 knockbackDirection = collision.contacts[0].normal;
            
            #if UNITY_2023_1_OR_NEWER
            playerRb.AddForce(knockbackDirection * collisionForce, ForceMode.Impulse);
            #else
            playerRb.AddForce(knockbackDirection * collisionForce, ForceMode.Impulse);
            #endif
        }

        // Reset color after a short delay
        Invoke(nameof(ResetVisual), 0.5f);
    }

    void ResetVisual()
    {
        if (obstacleRenderer != null)
        {
            obstacleRenderer.material.color = originalColor;
        }
        wasHit = false;
    }

    // Editor visualization
    void OnDrawGizmos()
    {
        Gizmos.color = wasHit ? hitColor : normalColor;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}