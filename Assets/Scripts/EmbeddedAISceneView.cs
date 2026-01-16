using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EmbeddedAISceneView : MonoBehaviour
{
    [Header("Scene & UI")]
    public string aiSceneName = "NuruAIScene";
    public GameObject aiPanel;     // The Panel with the RawImage
    public RawImage aiView;        // The RawImage in the panel
    public RenderTexture aiRenderTexture; // NuruAI_RT

    private bool aiSceneLoaded = false;

    // Called by your button to toggle the AI window
    public void ToggleAIWindow()
    {
        if (!aiSceneLoaded)
        {
            // First time: load scene additively and hook it up
            StartCoroutine(LoadAISceneAndInit());
        }
        else
        {
            // Next times: just show/hide the panel
            aiPanel.SetActive(!aiPanel.activeSelf);
        }
    }

    private IEnumerator LoadAISceneAndInit()
    {
        // Load AI scene additively (keeps Environment 3 alive)
        AsyncOperation op = SceneManager.LoadSceneAsync(aiSceneName, LoadSceneMode.Additive);
        while (!op.isDone)
            yield return null;

        // Get the loaded scene
        Scene aiScene = SceneManager.GetSceneByName(aiSceneName);

        if (!aiScene.IsValid())
        {
            Debug.LogError("[EmbeddedAISceneView] Failed to load AI scene: " + aiSceneName);
            yield break;
        }

        // Find a camera in the AI scene
        GameObject[] roots = aiScene.GetRootGameObjects();
        Camera aiCamera = null;
        foreach (var root in roots)
        {
            aiCamera = root.GetComponentInChildren<Camera>(true);
            if (aiCamera != null)
                break;
        }

        if (aiCamera == null)
        {
            Debug.LogError("[EmbeddedAISceneView] No camera found in AI scene!");
            yield break;
        }

        // Route that camera to our RenderTexture
        aiCamera.targetTexture = aiRenderTexture;

        // Show that RenderTexture inside our UI RawImage
        aiView.texture = aiRenderTexture;

        aiSceneLoaded = true;

        // Show the panel
        aiPanel.SetActive(true);

        Debug.Log("[EmbeddedAISceneView] AI scene loaded and embedded.");
    }
}
