using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TwoSceneSwitcher : MonoBehaviour
{
    // Call this from a button to load any scene by name
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // Convenience methods if you prefer fixed targets:

    public void LoadGameScene()
    {
        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }

    public void LoadAIScene()
    {
        SceneManager.LoadScene("NuruAIScene", LoadSceneMode.Single);
    }
}
