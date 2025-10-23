using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 1, -5);
    public float rotationSpeed = 15f;   // drag sensitivity
    public float returnSpeed = 2f;     // how fast it snaps back

    private float yaw = 0f;
    private float pitch = 0f;
    private Vector3 currentOffset;

    private void Start()
    {
        currentOffset = offset;
    }

    private void LateUpdate()
    {
        if (!target) return;

        if (Input.GetMouseButton(1)) // Hold RIGHT mouse to drag
        {
            yaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            yaw = Mathf.Clamp(yaw, -90f, 90f);
            pitch -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -40f, 50f);
        }
        else
        {
            // Smoothly return to default position (behind player)
            yaw = Mathf.Lerp(yaw, 0f, returnSpeed * Time.deltaTime);
            pitch = Mathf.Lerp(pitch, 0f, returnSpeed * Time.deltaTime);
        }

        // Calculate camera rotation relative to player's facing direction
        Quaternion playerRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        Quaternion finalRotation = playerRotation * cameraRotation;
        
        Vector3 desiredPosition = target.position + finalRotation * currentOffset;

        transform.position = desiredPosition;
        transform.LookAt(target.position + Vector3.up * 2f); // look slightly above player's feet
    }
}