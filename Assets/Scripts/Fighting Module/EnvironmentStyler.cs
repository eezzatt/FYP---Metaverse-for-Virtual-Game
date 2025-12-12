using UnityEngine;

public class EnvironmentStyler : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridSize = 20; // Number of tiles per side
    public float tileSize = 1f; // Size of each grid tile
    public Color gridColor1 = new Color(0.2f, 0.2f, 0.24f); // Darker grey
    public Color gridColor2 = new Color(0.27f, 0.27f, 0.31f); // Lighter grey
    
    void Start()
    {
        StyleEnvironment();
    }
    
    void StyleEnvironment()
    {
        // Create grid ground instead of single colored ground
        CreateGridGround();
        
        // Add colored walls to make the arena more interesting
        CreateColoredWall(new Vector3(0, 2.5f, 15), new Vector3(30, 5, 1), Color.grey);
        CreateColoredWall(new Vector3(0, 2.5f, -15), new Vector3(30, 5, 1), Color.grey);
        CreateColoredWall(new Vector3(15, 2.5f, 0), new Vector3(1, 5, 30), Color.grey);
        CreateColoredWall(new Vector3(-15, 2.5f, 0), new Vector3(1, 5, 30), Color.grey);
        
        // Color the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Renderer playerRenderer = player.GetComponent<Renderer>();
            if (playerRenderer != null)
            {
                Color c;
                ColorUtility.TryParseHtmlString("#007180ff", out c);
                playerRenderer.material.color = c;
            }
        }
        
        // Color the enemy
        GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");
        if (enemy != null)
        {
            Renderer enemyRenderer = enemy.GetComponent<Renderer>();
            if (enemyRenderer != null)
            {
                Color c;
                ColorUtility.TryParseHtmlString("#350000ff", out c);
                enemyRenderer.material.color = c;
            }
        }
    }
    
    void CreateGridGround()
    {
        // Create parent object for organization
        GameObject gridParent = new GameObject("GridGround");
        gridParent.transform.position = Vector3.zero;
        
        // Calculate starting position to center the grid
        float startX = -(gridSize * tileSize) / 2f + tileSize / 2f;
        float startZ = -(gridSize * tileSize) / 2f + tileSize / 2f;
        
        // Create grid tiles
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // Alternate colors in checkerboard pattern
                Color tileColor = ((x + z) % 2 == 0) ? gridColor1 : gridColor2;
                
                // Create tile
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"GridTile_{x}_{z}";
                tile.tag = "Ground";
                
                // Position tile
                Vector3 position = new Vector3(
                    startX + x * tileSize,
                    0f, // Ground level
                    startZ + z * tileSize
                );
                tile.transform.position = position;
                
                // Scale tile (make it flat like a floor tile)
                tile.transform.localScale = new Vector3(tileSize, 0.1f, tileSize);
                
                // Color tile
                Renderer tileRenderer = tile.GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    tileRenderer.material.color = tileColor;
                }
                
                // Parent to grid
                tile.transform.SetParent(gridParent.transform);
            }
        }
        
        Debug.Log($"Created grid ground with {gridSize * gridSize} tiles");
    }
    
    void CreateColoredWall(Vector3 position, Vector3 scale, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "ArenaWall";
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.tag = "Wall";
        
        Renderer wallRenderer = wall.GetComponent<Renderer>();
        if (wallRenderer != null)
        {
            wallRenderer.material.color = color;
        }
    }
}