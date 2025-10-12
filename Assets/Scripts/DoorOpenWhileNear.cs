using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DoorOpenWhileNear : MonoBehaviour
{
    public Animator doorAnimator;
    public string animatorBoolName = "isPlayerNear";
    public string playerTag = "Player";
    public Collider doorCollider; // solid door collider
    public string sceneToLoad; // name of the scene to load

    private bool playerInside = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (playerInside) return;

        // Player entered trigger
        playerInside = true;

        // Open door
        doorAnimator.SetBool(animatorBoolName, true);
        if (doorCollider != null)
            Invoke(nameof(DisableDoorCollider), 1f); // matches DoorOpen animation length

        // Start scene load if a scene name is set
        if (!string.IsNullOrEmpty(sceneToLoad))
            StartCoroutine(LoadSceneDelayed(1f)); // delay allows door animation to finish
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (!playerInside) return;

        // Player exited trigger
        playerInside = false;

        // Close door
        doorAnimator.SetBool(animatorBoolName, false);
        if (doorCollider != null)
            Invoke(nameof(EnableDoorCollider), 0.1f);
    }

    private void DisableDoorCollider()
    {
        if (doorCollider != null)
            doorCollider.enabled = false;
    }

    private void EnableDoorCollider()
    {
        if (doorCollider != null)
            doorCollider.enabled = true;
    }

    private IEnumerator LoadSceneDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneToLoad);
    }
}
