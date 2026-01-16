using UnityEngine;

public class InputBoxtrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject InputbOX;
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            InputbOX.SetActive(true);
            InputbOX.GetComponent<UnityEngine.UI.InputField>().ActivateInputField();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            InputbOX.SetActive(false);
            InputbOX.GetComponent<UnityEngine.UI.InputField>().DeactivateInputField();
        }
    }
}
