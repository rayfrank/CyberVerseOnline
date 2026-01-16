using UnityEngine;

public class DoorMechanics : MonoBehaviour
{
    public bool isLocked = true;
    public Animator doorAnimator;
    public GameObject doorOpenIndicator;
    private bool playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered door area.");
            playerInRange = true;
            doorOpenIndicator.SetActive(true);
            isLocked = false; // unlock when player enters
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player left door area.");
            playerInRange = false;
            doorOpenIndicator.SetActive(false);
        }

    }

    private void Update()
    {
        if (!isLocked && playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Door opening...");
            doorAnimator.SetBool("open", true);

        }
    }
}
