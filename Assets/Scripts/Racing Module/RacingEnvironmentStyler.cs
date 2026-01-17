using UnityEngine;

/// <summary>
/// Styles the racing game environment with a modern cyberpunk/neon aesthetic
/// Apply this to an empty GameObject in your racing scene
/// </summary>
public class RacingEnvironmentStyler : MonoBehaviour
{
    [Header("Track Colors")]
    [Tooltip("Main track surface color")]
    public Color trackColor;
    
    [Header("Wall Colors")]
    [Tooltip("First wall color for checkered pattern")]
    public Color firstWallColor;
    
    [Tooltip("Second wall color for checkered pattern")]
    public Color secondWallColor;
    
    [Header("Checkered Pattern Settings")]
    [Tooltip("Size of each checker square")]
    public float checkerSize;
    
    [Header("Car Colors")]
    [Tooltip("Player car primary color")]
    public Color carBodyColor;
    
    [Tooltip("Player car secondary/accent color")]
    public Color carWheelColor;

    void Start()
    {
        StyleRacingEnvironment();
    }

    void StyleRacingEnvironment()
    {
        Debug.Log("=== Starting Racing Environment Styling ===");
        
        // Style track pieces
        StyleTrackPieces();
        
        // Style walls with checkered pattern
        StyleWalls();
        
        // Style the player car
        StylePlayerCar();
        
        Debug.Log("=== Racing Environment Styling Complete ===");
    }

    void StyleTrackPieces()
    {
        // Find the Track parent object
        GameObject trackParent = GameObject.Find("Track");
        
        if (trackParent == null)
        {
            Debug.LogWarning("No 'Track' parent found. Make sure your track is under a parent named 'Track'");
            return;
        }

        int trackPiecesStyled = 0;
        
        // Style all track children
        foreach (Transform child in trackParent.transform)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material trackMat = CreateMaterial(trackColor);
                renderer.material = trackMat;
                trackPiecesStyled++;
            }
        }
        
        Debug.Log($"Styled {trackPiecesStyled} track pieces");
    }

    void StyleWalls()
    {
        // Find all objects tagged as "Wall"
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        
        int wallsStyled = 0;
        
        foreach (GameObject wall in walls)
        {
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Create checkered material using both colors
                Material checkeredMat = CreateCheckeredMaterial(firstWallColor, secondWallColor);
                renderer.material = checkeredMat;
                wallsStyled++;
            }
        }
        
        Debug.Log($"Styled {wallsStyled} walls with checkered pattern");
    }

    void StylePlayerCar()
    {
        GameObject carBody = GameObject.Find("CarBody");
        
        if (carBody == null)
        {
            Debug.LogWarning("No car body found");
            return;
        }

        // Get the main renderer
        Renderer carRenderer = carBody.GetComponent<Renderer>();
        
        if (carRenderer != null)
        {
            Material carMat = CreateMaterial(carBodyColor);            
            carRenderer.material = carMat;
            Debug.Log("Player car styled");
        }

        // If the car has child objects (wheels, spoiler, etc.), style them too
        GameObject[] wheels = GameObject.FindGameObjectsWithTag("Wheel");
        
        if (wheels.Length > 1) // More than just the main renderer
        {
            foreach (GameObject wheel in wheels)
            {
                if (wheel != carBody) // Skip the main body
                {
                    Renderer wheelRenderer = wheel.GetComponent<Renderer>();
                    Material accentMat = CreateMaterial(carWheelColor);
                    wheelRenderer.material = accentMat;
                }
            }
        }
    }

    /// <summary>
    /// Helper method to create a material with optional emission
    /// </summary>
    Material CreateMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        
        // Make it slightly reflective for a polished look
        mat.SetFloat("_Metallic", 0.2f);
        mat.SetFloat("_Glossiness", 0.6f);
        
        return mat;
    }

    /// <summary>
    /// Creates a checkered pattern material using two colors
    /// </summary>
    Material CreateCheckeredMaterial(Color color1, Color color2)
    {
        // Create a new material using URP/Lit shader
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // Create a checkered texture
        int textureSize = 256;
        Texture2D checkeredTexture = new Texture2D(textureSize, textureSize);
        
        // Calculate checker square size in pixels
        int checkerPixelSize = (int)Mathf.Max(1, checkerSize);
        
        // Fill the texture with checkered pattern
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // Determine which checker square this pixel belongs to
                int checkerX = x / checkerPixelSize;
                int checkerY = y / checkerPixelSize;
                
                // Alternate colors based on checker position
                bool useFirstColor = (checkerX + checkerY) % 2 == 0;
                checkeredTexture.SetPixel(x, y, useFirstColor ? color1 : color2);
            }
        }
        
        // Apply the texture
        checkeredTexture.Apply();
        checkeredTexture.filterMode = FilterMode.Point; // Keep sharp edges for checkers
        
        // Assign texture to material
        mat.mainTexture = checkeredTexture;
        
        // Set tiling to control checker size on the wall
        mat.mainTextureScale = new Vector2(1f, 1f);
        
        // Make it slightly reflective
        mat.SetFloat("_Metallic", 0.2f);
        mat.SetFloat("_Glossiness", 0.6f);
        
        return mat;
    }

    /// <summary>
    /// Call this to restyle the environment (useful for testing different color schemes)
    /// </summary>
    [ContextMenu("Restyle Environment")]
    public void RestyleEnvironment()
    {
        // Clear existing track lines
        GameObject linesParent = GameObject.Find("TrackLines");
        if (linesParent != null)
        {
            DestroyImmediate(linesParent);
        }
        
        StyleRacingEnvironment();
    }
}