using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Permanent world-space UI that stays on the plane in EDIT MODE and PLAY MODE.
/// Builds the full multi-panel UI (Home/Logs/Cameras/Console) with the same sizing as your current design.
/// Attach to your monitor Plane.
/// </summary>
[ExecuteAlways]
public class PermanentMonitorUI : MonoBehaviour
{
    [Header("Physical size on the monitor (meters)")]
    public Vector2 canvasSizeMeters = new Vector2(0.6f, 0.35f);

    [Header("Render sharpness")]
    [Range(200, 3000)] public int pixelsPerUnit = 1200;

    [Header("Sits slightly above the plane to avoid z-fighting")]
    public float surfaceOffset = 0.002f;

    [Header("Auto rebuild when values change")]
    public bool autoRebuild = true;

    [Header("Names")]
    public string rootName = "MonitorUI_ROOT";

    private Transform root;
    private Canvas canvas;

    void OnEnable()
    {
        EnsurePlaneHasCollider();
        BuildIfMissing();
        ApplySizing();
        EnsureEventSystemInScene();
        EnsureMainCameraPhysicsRaycaster();
    }

    void OnValidate()
    {
        if (!autoRebuild) return;
        BuildIfMissing();
        ApplySizing();
    }

    [ContextMenu("Rebuild Monitor UI")]
    public void RebuildNow()
    {
        DestroyExisting();
        BuildIfMissing();
        ApplySizing();
    }

    void BuildIfMissing()
    {
        root = transform.Find(rootName);
        if (root == null)
        {
            var go = new GameObject(rootName);
            go.transform.SetParent(transform, false);
            root = go.transform;

            // Position/rotation to sit on the plane like a screen
            root.localPosition = new Vector3(0f, surfaceOffset, 0f);
            root.localRotation = Quaternion.Euler(90f, 0f, 0f);
            root.localScale = Vector3.one;
        }

        canvas = root.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(root, false);

            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Build UI
            BuildUI(canvasGO.transform);
        }
        else
        {
            // If canvas exists but is missing panels (e.g. old version), rebuild cleanly.
            var existingPanel = canvas.transform.Find("Panel");
            var home = canvas.transform.Find("Panel_Home");
            if (existingPanel != null || home == null)
            {
                DestroyExisting();
                BuildIfMissing();
            }
        }
    }

    void ApplySizing()
    {
        if (!canvas) return;

        var canvasRect = canvas.GetComponent<RectTransform>();
        if (!canvasRect) return;

        // Size in pixels (UI units)
        canvasRect.sizeDelta = new Vector2(canvasSizeMeters.x * pixelsPerUnit,
                                           canvasSizeMeters.y * pixelsPerUnit);

        // Scale so that pixels map to meters in world
        float scale = 1f / pixelsPerUnit;
        canvas.transform.localScale = new Vector3(scale, scale, scale);

        // Keep it slightly above surface
        if (root != null)
            root.localPosition = new Vector3(0f, surfaceOffset, 0f);
    }

    void BuildUI(Transform parent)
    {
        // Root background panel (same look as before)
        GameObject rootPanel = CreateUIObject("Panel", parent);
        var rootImg = rootPanel.AddComponent<Image>();
        rootImg.color = new Color(0.08f, 0.10f, 0.12f, 0.95f);
        rootImg.raycastTarget = true;

        var prt = rootPanel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        // Title (same sizing/placement)
        GameObject title = CreateUIObject("Title", rootPanel.transform);
        var titleText = title.AddComponent<Text>();
        titleText.text = "CYBER LAB CONSOLE";
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 46;
        titleText.color = Color.white;

        var trt = title.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 1);
        trt.anchorMax = new Vector2(1, 1);
        trt.pivot = new Vector2(0.5f, 1);
        trt.anchoredPosition = new Vector2(0, -20);
        trt.sizeDelta = new Vector2(0, 80);

        // --------- Create panels (these are what your controller expects) ---------
        var panelHome = CreateFullPanel("Panel_Home", rootPanel.transform);
        var panelLogs = CreateFullPanel("Panel_Logs", rootPanel.transform);
        var panelCameras = CreateFullPanel("Panel_Cameras", rootPanel.transform);
        var panelConsole = CreateFullPanel("Panel_Console", rootPanel.transform);

        // Default: show Home only
        panelHome.SetActive(true);
        panelLogs.SetActive(false);
        panelCameras.SetActive(false);
        panelConsole.SetActive(false);

        // --------- HOME (same three buttons as before, same sizes/positions) ---------
        CreateButton(panelHome.transform, "Open Logs", new Vector2(0, -130));
        CreateButton(panelHome.transform, "Cameras", new Vector2(0, -220));
        CreateButton(panelHome.transform, "Access Console", new Vector2(0, -310));

        // Status (same as before)
        GameObject status = CreateUIObject("Status", panelHome.transform);
        var statusText = status.AddComponent<Text>();
        statusText.text = "Status: Idle (Edit Mode Visible)";
        statusText.alignment = TextAnchor.LowerCenter;
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 28;
        statusText.color = new Color(0.8f, 0.9f, 1f, 1f);

        var srt = status.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 0);
        srt.anchorMax = new Vector2(1, 0);
        srt.pivot = new Vector2(0.5f, 0);
        srt.anchoredPosition = new Vector2(0, 15);
        srt.sizeDelta = new Vector2(0, 60);

        // --------- LOGS PANEL ---------
        // Title label (kept similar to your style)
        CreateHeaderLabel(panelLogs.transform, "SYSTEM LOGS", new Vector2(0, -120), 34);

        // LogsText
        var logsTextGO = CreateUIObject("LogsText", panelLogs.transform);
        var logsText = logsTextGO.AddComponent<Text>();
        logsText.text = "Logs will appear here...";
        logsText.alignment = TextAnchor.UpperLeft;
        logsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        logsText.fontSize = 22;
        logsText.color = new Color(0.85f, 0.9f, 1f, 1f);
        logsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logsText.verticalOverflow = VerticalWrapMode.Overflow;

        var lrt = logsTextGO.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.5f, 0.5f);
        lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.anchoredPosition = new Vector2(0, -10);
        lrt.sizeDelta = new Vector2(900, 300);

        // Back button
        CreateSmallButton(panelLogs.transform, "Back", new Vector2(-360, -120));

        // --------- CAMERAS PANEL ---------
        CreateHeaderLabel(panelCameras.transform, "CAMERAS", new Vector2(0, -120), 34);

        // Camera feedback
        var camFeedGO = CreateUIObject("CameraFeedbackText", panelCameras.transform);
        var camFeedText = camFeedGO.AddComponent<Text>();
        camFeedText.text = "Tip: Disable only after console access.";
        camFeedText.alignment = TextAnchor.MiddleCenter;
        camFeedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        camFeedText.fontSize = 24;
        camFeedText.color = new Color(0.8f, 0.9f, 1f, 1f);

        var cfrt = camFeedGO.GetComponent<RectTransform>();
        cfrt.anchorMin = new Vector2(0.5f, 1);
        cfrt.anchorMax = new Vector2(0.5f, 1);
        cfrt.pivot = new Vector2(0.5f, 1);
        cfrt.anchoredPosition = new Vector2(0, -190);
        cfrt.sizeDelta = new Vector2(900, 60);

        // Disable cameras button
        CreateButton(panelCameras.transform, "Disable Cameras", new Vector2(0, -260));

        // Back button
        CreateSmallButton(panelCameras.transform, "Back", new Vector2(-360, -120));

        // --------- CONSOLE PANEL ---------
        CreateHeaderLabel(panelConsole.transform, "CONSOLE ACCESS", new Vector2(0, -120), 34);

        // Console feedback
        var consoleFeedGO = CreateUIObject("ConsoleFeedbackText", panelConsole.transform);
        var consoleFeedText = consoleFeedGO.AddComponent<Text>();
        consoleFeedText.text = "Select credential and attempt login.";
        consoleFeedText.alignment = TextAnchor.MiddleCenter;
        consoleFeedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        consoleFeedText.fontSize = 24;
        consoleFeedText.color = new Color(0.8f, 0.9f, 1f, 1f);

        var conFrt = consoleFeedGO.GetComponent<RectTransform>();
        conFrt.anchorMin = new Vector2(0.5f, 1);
        conFrt.anchorMax = new Vector2(0.5f, 1);
        conFrt.pivot = new Vector2(0.5f, 1);
        conFrt.anchoredPosition = new Vector2(0, -190);
        conFrt.sizeDelta = new Vector2(900, 60);

        // Dropdown (legacy Dropdown)
        CreateDropdown(panelConsole.transform, "CredentialDropdown", new Vector2(0, -250), new Vector2(520, 60),
            new string[] { "GUEST_BAD", "TEMP_BAD", "ADMIN_OK" });

        // Attempt login button
        CreateButton(panelConsole.transform, "Attempt Login", new Vector2(0, -330));

        // Back button
        CreateSmallButton(panelConsole.transform, "Back", new Vector2(-360, -120));
    }

    // Creates a full-screen panel under root panel (fills the area)
    GameObject CreateFullPanel(string name, Transform parent)
    {
        var go = CreateUIObject(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Keep panels transparent (root background stays)
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;

        return go;
    }

    void CreateHeaderLabel(Transform parent, string text, Vector2 anchoredPos, int fontSize)
    {
        var go = CreateUIObject(text.Replace(" ", "_") + "_Header", parent);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.alignment = TextAnchor.UpperCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = Color.white;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(900, 60);
    }

    Button CreateButton(Transform parent, string label, Vector2 anchoredPos)
    {
        GameObject btnObj = CreateUIObject(label.Replace(" ", "_") + "_Button", parent);
        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.16f, 0.18f, 0.22f, 1f);
        img.raycastTarget = true;

        var btn = btnObj.AddComponent<Button>();

        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(520, 74); // SAME size as your original

        GameObject textObj = CreateUIObject("Text", btnObj.transform);
        var t = textObj.AddComponent<Text>();
        t.text = label;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 30; // SAME as your original
        t.color = Color.white;

        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return btn;
    }

    Button CreateSmallButton(Transform parent, string label, Vector2 anchoredPos)
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
        rt.sizeDelta = new Vector2(180, 58); // smaller back button

        GameObject textObj = CreateUIObject("Text", btnObj.transform);
        var t = textObj.AddComponent<Text>();
        t.text = label;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 26;
        t.color = Color.white;

        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return btn;
    }

    Dropdown CreateDropdown(Transform parent, string name, Vector2 anchoredPos, Vector2 size, string[] options)
    {
        // Root
        GameObject go = CreateUIObject(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.13f, 0.16f, 1f);

        var dd = go.AddComponent<Dropdown>();

        // Label
        var labelGO = CreateUIObject("Label", go.transform);
        var label = labelGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 26;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;

        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(18, 0);
        lrt.offsetMax = new Vector2(-55, 0);

        // Arrow
        var arrowGO = CreateUIObject("Arrow", go.transform);
        var arrow = arrowGO.AddComponent<Text>();
        arrow.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        arrow.fontSize = 34;
        arrow.color = Color.white;
        arrow.alignment = TextAnchor.MiddleCenter;
        arrow.text = "▼";

        var art = arrowGO.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(1, 0);
        art.anchorMax = new Vector2(1, 1);
        art.pivot = new Vector2(1, 0.5f);
        art.sizeDelta = new Vector2(50, 0);
        art.anchoredPosition = new Vector2(-10, 0);

        // Template
        var templateGO = CreateUIObject("Template", go.transform);
        templateGO.SetActive(false);
        var templateRT = templateGO.GetComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1);
        templateRT.anchoredPosition = new Vector2(0, -5);
        templateRT.sizeDelta = new Vector2(0, 220);

        var templateImg = templateGO.AddComponent<Image>();
        templateImg.color = new Color(0.10f, 0.11f, 0.14f, 1f);

        // Viewport
        var viewportGO = CreateUIObject("Viewport", templateGO.transform);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;

        var viewportMask = viewportGO.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        var viewportImg = viewportGO.AddComponent<Image>();
        viewportImg.color = new Color(0, 0, 0, 0.25f);

        // Content
        var contentGO = CreateUIObject("Content", viewportGO.transform);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Item (required by dropdown)
        var itemGO = CreateUIObject("Item", contentGO.transform);
        itemGO.AddComponent<Toggle>();
        var itemImg = itemGO.AddComponent<Image>();
        itemImg.color = new Color(0.16f, 0.18f, 0.22f, 1f);

        var itemLabelGO = CreateUIObject("Item Label", itemGO.transform);
        var itemLabel = itemLabelGO.AddComponent<Text>();
        itemLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        itemLabel.fontSize = 24;
        itemLabel.color = Color.white;
        itemLabel.alignment = TextAnchor.MiddleLeft;

        var ilrt = itemLabelGO.GetComponent<RectTransform>();
        ilrt.anchorMin = Vector2.zero;
        ilrt.anchorMax = Vector2.one;
        ilrt.offsetMin = new Vector2(18, 0);
        ilrt.offsetMax = new Vector2(-18, 0);

        // Assign dropdown refs
        dd.targetGraphic = img;
        dd.captionText = label;
        dd.template = templateRT;
        dd.itemText = itemLabel;

        dd.options.Clear();
        foreach (var opt in options)
            dd.options.Add(new Dropdown.OptionData(opt));
        dd.value = 0;
        dd.RefreshShownValue();

        return dd;
    }

    GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    void DestroyExisting()
    {
        var existing = transform.Find(rootName);
        if (existing == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.DestroyObjectImmediate(existing.gameObject);
        else
            Destroy(existing.gameObject);
#else
        Destroy(existing.gameObject);
#endif
    }

    void EnsurePlaneHasCollider()
    {
        if (!TryGetComponent<Collider>(out _))
            gameObject.AddComponent<BoxCollider>();
    }

    void EnsureEventSystemInScene()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>(); // old input system

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
#endif
    }

    void EnsureMainCameraPhysicsRaycaster()
    {
        var cam = Camera.main;
        if (!cam) return;
        if (!cam.TryGetComponent<PhysicsRaycaster>(out _))
            cam.gameObject.AddComponent<PhysicsRaycaster>();
    }
}
