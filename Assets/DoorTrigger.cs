using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public Animator doorAnimator;
    private bool isOpened = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!isOpened && other.CompareTag("Player"))
        {
            doorAnimator.SetTrigger("Open");
            isOpened = true;
        }
    }
}
