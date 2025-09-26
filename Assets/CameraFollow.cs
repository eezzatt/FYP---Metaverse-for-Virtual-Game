using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;          // Player transform
    public Vector3 offset = new Vector3(0, 5, -7); // Offset relative to player
    public float smoothSpeed = 100f;    // Camera follow speed

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Rotate offset based on player's rotation
        Vector3 rotatedOffset = target.rotation * offset;

        // 2. Desired camera position = player position + rotated offset
        Vector3 desiredPosition = target.position + rotatedOffset;

        // 3. Smoothly move camera to desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 4. Always look at the player
        transform.LookAt(target.position + Vector3.up * 1.5f); // look slightly above player center
    }
}
