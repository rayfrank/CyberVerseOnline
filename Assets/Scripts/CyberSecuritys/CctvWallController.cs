using UnityEngine;

public class CctvWallController : MonoBehaviour
{
    [Header("Assign monitors manually OR auto-find")]
    public GameObject[] tvDisplays; // drag all TVDisplay_small here
    public bool autoFindOnAwake = true;

    [Tooltip("If auto-find is ON, it will collect every object named 'TVDisplay_small' in the scene.")]
    public string tvDisplayName = "TVDisplay_small";

    void Awake()
    {
        if (autoFindOnAwake)
            AutoFindDisplays();
    }

    [ContextMenu("Auto-Find TV Displays")]
    public void AutoFindDisplays()
    {
        // Finds ALL transforms named TVDisplay_small in the scene (even if inactive in editor they are usually active)
        var all = FindObjectsOfType<Transform>(true);
        var list = new System.Collections.Generic.List<GameObject>();

        foreach (var t in all)
        {
            if (t.name == tvDisplayName)
                list.Add(t.gameObject);
        }

        tvDisplays = list.ToArray();
        Debug.Log($"CctvWallController: Found {tvDisplays.Length} displays named '{tvDisplayName}'.");
    }

    public void DisableAll()
    {
        SetAllActive(false);
    }

    public void EnableAll()
    {
        SetAllActive(true);
    }

    public void SetAllActive(bool active)
    {
        if (tvDisplays == null) return;

        int changed = 0;
        foreach (var go in tvDisplays)
        {
            if (!go) continue;
            go.SetActive(active);
            changed++;
        }

        Debug.Log($"CctvWallController: SetActive({active}) on {changed} displays.");
    }
}
