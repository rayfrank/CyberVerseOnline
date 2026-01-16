using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MonitorWorldCanvas : MonoBehaviour
{
    [Header("Sizing (meters in world space)")]
    public Vector2 canvasSizeMeters = new Vector2(0.6f, 0.35f); // width, height
    public float canvasOffset = 0.002f; // push slightly above plane to avoid z-fighting
    public int canvasPixelsPerUnit = 1000; // higher = sharper UI

    private Canvas canvas;
    private RectTransform canvasRect;

    void Awake()
    {
        EnsureSceneHasEventSystem();
        EnsureMainCameraHasPhysicsRaycaster();
        EnsurePlaneHasCollider();

        BuildWorldCanvas();
        BuildSampleUI();
    }

    void BuildWorldCanvas()
    {
        var go = new GameObject("MonitorCanvas");
        go.transform.SetParent(transform, worldPositionStays: false);

        // Align canvas to sit on top of the plane
        // Plane's normal is its local +Y, so we place the canvas slightly above +Y.
        go.transform.localPosition = new Vector3(0f, canvasOffset, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // make it face outward like a screen
        go.transform.localScale = Vector3.one;

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Add raycaster so UI elements can be clicked
        go.AddComponent<GraphicRaycaster>();

        canvasRect = go.GetComponent<RectTransform>();
        // RectTransform size is in "UI units". With WorldSpace, scale controls physical size.
        // Easiest: keep scale at 0.001 and set rect size in pixels.
        // We'll set physical size using localScale based on desired meters.
        canvasRect.sizeDelta = new Vector2(canvasSizeMeters.x * canvasPixelsPerUnit,
                                           canvasSizeMeters.y * canvasPixelsPerUnit);

        float scale = 1f / canvasPixelsPerUnit; // converts pixels to meters roughly
        go.transform.localScale = new Vector3(scale, scale, scale);
    }

    void BuildSampleUI()
    {
        // Full-screen background panel
        GameObject panel = CreateUIObject("Panel", canvas.transform);
        var img = panel.AddComponent<Image>();
        img.raycastTarget = true; // must be true to catch clicks on panel
        img.color = new Color(0.08f, 0.10f, 0.12f, 0.92f);

        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // Title
        GameObject title = CreateUIObject("Title", panel.transform);
        var titleText = title.AddComponent<Text>();
        titleText.text = "CAMERA CONSOLE";
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 46;
        titleText.color = Color.white;

        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -20);
        titleRt.sizeDelta = new Vector2(0, 80);

        // Button 1
        var b1 = CreateButton(panel.transform, "View Logs", new Vector2(0, -130));
        b1.onClick.AddListener(() => Debug.Log("Monitor: View Logs clicked"));

        // Button 2
        var b2 = CreateButton(panel.transform, "Disable Cameras (SIM)", new Vector2(0, -220));
        b2.onClick.AddListener(() => Debug.Log("Monitor: Disable Cameras clicked"));

        // Status text
        GameObject status = CreateUIObject("Status", panel.transform);
        var statusText = status.AddComponent<Text>();
        statusText.text = "Status: Connected";
        statusText.alignment = TextAnchor.LowerCenter;
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 28;
        statusText.color = new Color(0.8f, 0.9f, 1f, 1f);

        var statusRt = status.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0, 0);
        statusRt.anchorMax = new Vector2(1, 0);
        statusRt.pivot = new Vector2(0.5f, 0);
        statusRt.anchoredPosition = new Vector2(0, 15);
        statusRt.sizeDelta = new Vector2(0, 60);
    }

    Button CreateButton(Transform parent, string label, Vector2 anchoredPos)
    {
        GameObject btnObj = CreateUIObject(label + "_Button", parent);
        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.16f, 0.18f, 0.22f, 1f);
        img.raycastTarget = true;

        var btn = btnObj.AddComponent<Button>();

        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(520, 74);

        // Text child
        GameObject textObj = CreateUIObject("Text", btnObj.transform);
        var t = textObj.AddComponent<Text>();
        t.text = label;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 30;
        t.color = Color.white;

        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return btn;
    }

    GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, worldPositionStays: false);
        go.AddComponent<RectTransform>();
        return go;
    }

    void EnsureSceneHasEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>(); // if you use the old input system
    }

    void EnsureMainCameraHasPhysicsRaycaster()
    {
        var cam = Camera.main;
        if (!cam) return;
        if (!cam.TryGetComponent<PhysicsRaycaster>(out _))
            cam.gameObject.AddComponent<PhysicsRaycaster>();
    }

    void EnsurePlaneHasCollider()
    {
        if (!TryGetComponent<Collider>(out _))
            gameObject.AddComponent<BoxCollider>();
    }
}
