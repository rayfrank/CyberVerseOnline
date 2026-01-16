using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BasicMenuOptions{
public class MenuManager : MonoBehaviour {

    [Header("Loading UI"),SerializeField]
    private GameObject UI;

    [SerializeField]
    private Slider loadingSlider;

    public void OpenSceneWithoutLoadingUI(string levelname)
    {
        SceneManager.LoadScene(levelname);
    }

    public void ButtonQuitGame()
    {
        Application.Quit();
    }

    public void DeletePlayerPreps()
    {
        PlayerPrefs.DeleteAll();
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadScene_Coroutine(sceneName));
    }

    public IEnumerator LoadScene_Coroutine(string sceneName)
    {
        loadingSlider.value = 0.1f;

        UI.SetActive(true);

        Time.timeScale = 1;

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        asyncOperation.allowSceneActivation = false;

        float progress = 0;

        while (!asyncOperation.isDone)
        {
            progress = Mathf.MoveTowards(progress, asyncOperation.progress, Time.deltaTime);
            loadingSlider.value = progress;

            if (progress >= 0.9f)
            {
                loadingSlider.value = 1;
                asyncOperation.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
}
