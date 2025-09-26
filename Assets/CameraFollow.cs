using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 1, -5);
    public float rotationSpeed = 2f;   // drag sensitivity
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

        if (Input.GetMouseButton(0)) // Hold left mouse to drag
        {
            yaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            yaw = Mathf.Clamp(yaw, -90f, 90f);
            pitch -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -40f, 50f);
        }
        else
        {
            // Smoothly return to player’s facing direction
            yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, returnSpeed * Time.deltaTime);
            pitch = Mathf.Lerp(pitch, 0f, returnSpeed * Time.deltaTime); // default angle
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = target.position + rotation * currentOffset;

        transform.position = desiredPosition;
        transform.LookAt(target.position + Vector3.up * 2f); // look slightly above player’s feet
    }
}
