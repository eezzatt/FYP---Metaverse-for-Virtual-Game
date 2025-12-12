using UnityEngine;

public class SimpleCharacterBuilder : MonoBehaviour
{
    [Header("Character Colors")]
    public Color skinColor = new Color(1f, 0.8f, 0.6f); // Peach skin
    public Color shirtColor = Color.blue;
    public Color pantsColor = new Color(0.2f, 0.4f, 0.2f); // Dark green
    
    [Header("Build on Start")]
    public bool buildOnStart = true;
    
    private GameObject originalCapsule;

    void Start()
    {
        if (buildOnStart)
        {
            BuildRobloxCharacter();
        }
    }
    
    public void BuildRobloxCharacter()
    {
        // Hide the original capsule but keep it for collision
        originalCapsule = GetComponentInChildren<Renderer>()?.gameObject;
        if (originalCapsule != null)
        {
            Renderer capsuleRenderer = originalCapsule.GetComponent<Renderer>();
            if (capsuleRenderer != null)
            {
                capsuleRenderer.enabled = false; // Hide but keep collider
            }
        }
        
        // Head
        GameObject head = CreateBodyPart(PrimitiveType.Cube, "Head", 
            new Vector3(0, 0.9f, 0), new Vector3(0.7f, 0.7f, 0.7f), skinColor);
        
        // Torso
        GameObject torso = CreateBodyPart(PrimitiveType.Cube, "Torso",
            new Vector3(0, 0, 0), new Vector3(0.9f, 1.1f, 0.5f), shirtColor);
        
        // Left Arm
        GameObject leftArm = CreateBodyPart(PrimitiveType.Cube, "LeftArm",
            new Vector3(-0.55f, 0.1f, 0), new Vector3(0.3f, 0.9f, 0.3f), shirtColor);
        
        // Right Arm (this will be the weapon arm)
        GameObject rightArm = CreateBodyPart(PrimitiveType.Cube, "RightArm",
            new Vector3(0.55f, 0.1f, 0), new Vector3(0.3f, 0.9f, 0.3f), shirtColor);
        
        // Left Leg
        GameObject leftLeg = CreateBodyPart(PrimitiveType.Cube, "LeftLeg",
            new Vector3(-0.25f, -0.85f, 0), new Vector3(0.35f, 0.8f, 0.35f), pantsColor);
        
        // Right Leg
        GameObject rightLeg = CreateBodyPart(PrimitiveType.Cube, "RightLeg",
            new Vector3(0.25f, -0.85f, 0), new Vector3(0.35f, 0.8f, 0.35f), pantsColor);
        
        // Optional: Add a simple weapon (sword/stick)
        GameObject weapon = CreateBodyPart(PrimitiveType.Cube, "Weapon",
            new Vector3(0.55f, -0.3f, 0.3f), new Vector3(0.15f, 1.2f, 0.15f), Color.gray);
        weapon.transform.parent = rightArm.transform;
        
        Debug.Log("Roblox-style character built!");
    }
    
    GameObject CreateBodyPart(PrimitiveType type, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.parent = transform;
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;
        
        // Set color
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
        
        // Remove collider to avoid physics conflicts (main capsule handles collision)
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
        return part;
    }
}