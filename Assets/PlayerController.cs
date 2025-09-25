using UnityEngine;
using System.IO;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector3 lastPosition;
    private int score = 0;

    private DataLogger logger;

    private void Start()
    {
        lastPosition = transform.position;
        logger = FindFirstObjectByType<DataLogger>(); // or assign via Inspector
        logger.Log("Start", transform.position, score);
    }

    private void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(moveX, 0f, moveZ);

        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);

        if (move != Vector3.zero && Vector3.Distance(transform.position, lastPosition) > 0.1f)
        {
            logger.Log("Move", transform.position, score);
            lastPosition = transform.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectible"))
        {
            score++;
            Destroy(other.gameObject);
            logger.Log("Collect", transform.position, score);
        }
    }
}
