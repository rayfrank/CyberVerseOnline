using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSelectManager : MonoBehaviour
{
    public void OpenAIScene()
    {
        SceneManager.LoadScene("NuruAIScene");
    }
    public void OpenCampaignScene()
    {
        SceneManager.LoadScene("campaign");
    }
}
