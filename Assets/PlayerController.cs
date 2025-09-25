using UnityEngine;
using System.IO;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed of movement
    private Vector3 lastPosition;     // Last position logged
    private int score = 0;
    private string filePath;


    private void Update()
    {
        // Get input from WASD or arrow keys
        float moveX = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float moveZ = Input.GetAxis("Vertical");   // W/S or Up/Down

        // Calculate movement direction
        Vector3 move = new Vector3(moveX, 0f, moveZ);

        // Apply movement
        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);

        if (move != Vector3.zero)
            {
                // Only log if player moved a significant distance or direction changed
                if (Vector3.Distance(transform.position, lastPosition) > 0.1f)
                {
                    string log = Time.time + ",Move," + transform.position.x + "," + transform.position.y + "," + transform.position.z + "," + score;
                    File.AppendAllText(filePath, log + "\n");

                    lastPosition = transform.position;   // Update last logged position
                }
            }

    }


    // This method is called when your player enters a trigger collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectible"))
        {
            score++;
            Debug.Log("Score: " + score);

            // Remove the collected object
            Destroy(other.gameObject);
        }

        string log = Time.time + ",Collect," + transform.position.x + "," +
        transform.position.y + "," + transform.position.z + "," + score;

        File.AppendAllText(filePath, log + "\n");
    }


    // Initialize file path
    private void Start()
    {
        lastPosition = transform.position;

        // This creates a CSV in your project's root folder
        filePath = Application.dataPath + "/PlayerLog.csv";

        // Write header line if the file doesn't exist yet
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Time,Action,PosX,PosY,PosZ,Score\n");
        }
        Debug.Log("Logging to PlayerLog.csv");
    }
}
