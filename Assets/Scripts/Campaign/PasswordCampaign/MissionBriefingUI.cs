using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Persistent Mission Briefing UI.
/// - Builds the Canvas in EDIT MODE (so it remains after exiting Play Mode).
/// - In Play Mode, we re-wire button listeners at runtime (Edit-mode listeners don't persist).
/// - Kenyan-themed header accents + optional kitenge pattern (clipped behind text).
/// </summary>
[ExecuteAlways]
public class PhishingMissionBriefingUI : MonoBehaviour
{
    [Header("Persistence")]
    public bool buildInEditMode = true;        // keeps UI after play mode exit (builds as scene child)
    public bool autoRebuildIfMissing = true;

    [Header("Popup behavior")]
    public bool showOnStart = true;
    public bool closeWithEnterSpaceEsc = true;
    public bool forceCursorForUI = true;

    [Header("Kenyan Theme")]
    public bool enableKitengePattern = true;
    [Range(0f, 0.3f)] public float patternOpacity = 0.18f;
    [Range(10, 40)] public int patternTileSize = 22;
    [Range(60, 180)] public int patternSpacingX = 95;
    [Range(40, 140)] public int patternSpacingY = 70;

    [Header("Animation")]
    public bool animateIn = true;
    public float animateDuration = 0.22f;

    [Header("Typewriter (optional)")]
    public bool typewriter = true;
    public float typewriterCPS = 60f;

    [Header("Content")]
    public string headerTitle = "Mission Briefing";
    public string missionName = "Operation: Phishproof";

    [TextArea(2, 6)]
    public string intro =
        "Karibu! You are about to use a simulated computer to learn phishing safety.\n" +
        "Your choices determine what happens next.";

    [TextArea(2, 8)] public string step1 =
        "Create an account and choose a strong password.\n\n" +
        "Use:\n- 12–16+ characters\n- Mix upper/lower + numbers + symbols\n- Avoid common words";

    [TextArea(2, 8)] public string step2 =
        "Open the Email app and inspect the suspicious message.\n\n" +
        "Look for:\n- Urgency or threats\n- Generic greeting\n- Strange sender/spelling";

    [TextArea(2, 8)] public string step3 =
        "Verify the domain carefully.\n\n" +
        "Official example: kca-security.co.ke\n" +
        "Suspicious example: verify.kca-security.com\n\n" +
        "If the domain mismatches, don't enter your password.";

    [TextArea(2, 8)] public string step4 =
        "Use the SAFE path:\n\n" +
        "Settings → Official Portal\n" +
        "Type the correct URL yourself instead of clicking links.";

    [TextArea(2, 8)] public string step5 =
        "Watch for alerts (e.g., a later 'New login from another country').\n\n" +
        "That is a clue your password may be compromised.";

    // ===== internal refs =====
    private const string CanvasName = "__PhishingBriefingCanvas_PERSISTENT";
    private Canvas canvas;
    private RectTransform panelRT;

    private TMP_Text stepTitleText, stepBodyText, pagerText;
    private Button backBtn, nextBtn, startBtn, skipBtn;

    private int stepIndex;
    private Step[] steps;
    private Coroutine textAnim;

    private CursorLockMode prevLock;
    private bool prevVisible;

    // Kenyan palette
    private readonly Color colBackdrop = new Color32(0, 0, 0, 180);
    private readonly Color colPanel = new Color32(247, 246, 242, 255);
    private readonly Color colBorder = new Color32(210, 214, 220, 255);
    private readonly Color colText = new Color32(22, 24, 26, 255);
    private readonly Color colMuted = new Color32(88, 94, 102, 255);

    private readonly Color colKenyaBlack = new Color32(22, 22, 22, 255);
    private readonly Color colKenyaRed = new Color32(183, 28, 28, 255);
    private readonly Color colKenyaGreen = new Color32(0, 121, 64, 255);
    private readonly Color colKenyaWhite = new Color32(245, 245, 245, 255);

    private readonly Color colPrimary = new Color32(0, 121, 64, 255);
    private readonly Color colPrimaryText = new Color32(255, 255, 255, 255);
    private readonly Color colSecondary = new Color32(235, 237, 241, 255);

    private struct Step { public string title; public string body; }

    // =========================
    // Unity hooks
    // =========================
    void OnEnable()
    {
        if (!autoRebuildIfMissing) return;

        // Build / find UI
        if (!Application.isPlaying && buildInEditMode)
        {
            BuildOrFindPersistentUI(editModePersistent: true);
            EnsureKitengeExists();
        }
        else if (Application.isPlaying)
        {
            EnsureEventSystemRuntime();
            BuildOrFindPersistentUI(editModePersistent: false);
            EnsureKitengeExists();
            WireButtonsRuntime(); // IMPORTANT: re-wire listeners in play mode
        }
    }

    void Start()
    {
        if (!Application.isPlaying) return;

        EnsureEventSystemRuntime();
        BuildOrFindPersistentUI(editModePersistent: false);
        EnsureKitengeExists();
        WireButtonsRuntime();

        BuildSteps();

        if (showOnStart)
        {
            Show(true);
            GoToStep(0, instant: true);
        }
        else
        {
            Show(false);
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (!canvas || !canvas.gameObject.activeSelf) return;

        if (closeWithEnterSpaceEsc)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (IsLastStep()) Finish();
                else Next();
            }
        }
    }

    // =========================
    // Public controls
    // =========================
    public void Show(bool show)
    {
        BuildOrFindPersistentUI(editModePersistent: !Application.isPlaying);
        EnsureKitengeExists();

        if (!canvas) return;

        canvas.gameObject.SetActive(show);

        if (Application.isPlaying && forceCursorForUI)
        {
            if (show) SaveAndForceCursor();
            else RestoreCursor();
        }

        if (Application.isPlaying && show)
        {
            WireButtonsRuntime();

            // reset to first step every time it opens (feels like a proper briefing)
            if (steps == null || steps.Length == 0) BuildSteps();
            GoToStep(0, instant: true);

            if (animateIn && panelRT)
            {
                panelRT.localScale = Vector3.one * 0.94f;
                SetCanvasGroupAlpha(panelRT.gameObject, 0f);
                StopAllCoroutines();
                StartCoroutine(AnimateIn());
            }
        }
    }

    public void Close()
    {
        if (textAnim != null) { StopCoroutine(textAnim); textAnim = null; }
        if (canvas) canvas.gameObject.SetActive(false);

        if (Application.isPlaying && forceCursorForUI)
            RestoreCursor();
    }

    // =========================
    // Steps
    // =========================
    void BuildSteps()
    {
        steps = new[]
        {
            new Step { title = "Welcome (Karibu)", body = intro },
            new Step { title = "Step 1: Create an Account", body = step1 },
            new Step { title = "Step 2: Inspect the Email", body = step2 },
            new Step { title = "Step 3: Verify the Domain", body = step3 },
            new Step { title = "Step 4: Use the Safe Path", body = step4 },
            new Step { title = "Step 5: Watch for Alerts", body = step5 },
            new Step { title = "Ready", body =
                "You are ready to start.\n\n" +
                "Remember:\n- Don't trust urgency\n- Check the domain\n- Use official portals\n- Strong passwords + MFA"
            }
        };

        stepIndex = Mathf.Clamp(stepIndex, 0, steps.Length - 1);
        UpdateNav();
    }

    void GoToStep(int i, bool instant)
    {
        if (steps == null || steps.Length == 0) return;

        stepIndex = Mathf.Clamp(i, 0, steps.Length - 1);

        if (stepTitleText) stepTitleText.text = steps[stepIndex].title;
        if (pagerText) pagerText.text = $"{stepIndex + 1}/{steps.Length}";

        if (textAnim != null) { StopCoroutine(textAnim); textAnim = null; }

        if (!typewriter || instant)
        {
            if (stepBodyText) stepBodyText.text = steps[stepIndex].body;
        }
        else
        {
            if (stepBodyText)
            {
                stepBodyText.text = "";
                textAnim = StartCoroutine(Typewriter(stepBodyText, steps[stepIndex].body, typewriterCPS));
            }
        }

        UpdateNav();
    }

    void Next()
    {
        if (IsLastStep()) return;
        GoToStep(stepIndex + 1, instant: false);
    }

    void Back()
    {
        if (stepIndex <= 0) return;
        GoToStep(stepIndex - 1, instant: true);
    }

    void Finish()
    {
        // Close completely when done
        Close();
    }

    bool IsLastStep() => steps != null && stepIndex >= steps.Length - 1;

    void UpdateNav()
    {
        if (!backBtn || !nextBtn || !startBtn || !skipBtn) return;

        bool last = IsLastStep();

        backBtn.gameObject.SetActive(stepIndex > 0);
        nextBtn.gameObject.SetActive(!last);
        startBtn.gameObject.SetActive(last);

        // label "Start Mission" only on last step, Next otherwise (we keep them separate buttons)
        var nextLabel = nextBtn.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (nextLabel) nextLabel.text = "Next →";
        var startLabel = startBtn.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (startLabel) startLabel.text = "Start Mission →";
    }

    // =========================
    // Runtime wiring (fixes non-working buttons)
    // =========================
    void WireButtonsRuntime()
    {
        if (!Application.isPlaying) return;
        if (!backBtn || !nextBtn || !startBtn || !skipBtn) return;

        backBtn.onClick.RemoveAllListeners();
        nextBtn.onClick.RemoveAllListeners();
        startBtn.onClick.RemoveAllListeners();
        skipBtn.onClick.RemoveAllListeners();

        backBtn.onClick.AddListener(() => Back());
        nextBtn.onClick.AddListener(() => Next());
        startBtn.onClick.AddListener(() => Finish());
        skipBtn.onClick.AddListener(() => Close());
    }

    // =========================
    // Build persistent UI (Edit Mode + Play Mode)
    // =========================
    void BuildOrFindPersistentUI(bool editModePersistent)
    {
        // Find existing canvas under THIS object (persists in scene)
        var existing = transform.Find(CanvasName);
        if (existing)
        {
            CacheUIRefs(existing);
            return;
        }

        // In edit mode: only create if allowed
        if (!Application.isPlaying && !buildInEditMode) return;

        // Create as a CHILD of this object so Unity saves it in the scene
        var canvasGO = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

#if UNITY_EDITOR
        if (!Application.isPlaying && editModePersistent)
        {
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Mission Briefing UI");
            EditorUtility.SetDirty(gameObject);
        }
#endif

        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var root = UI.MakeRect(canvasGO.transform, "Root", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var backdrop = UI.Panel(root, "Backdrop", colBackdrop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        backdrop.GetComponent<Image>().raycastTarget = true; // captures clicks so you don't click the world

        panelRT = UI.Panel(root, "Panel", colPanel,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-560, -360), new Vector2(560, 360));
        UI.AddSoftOutline(panelRT.gameObject, colBorder, 2);

        // Header
        var header = UI.Panel(panelRT, "Header", colKenyaBlack,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -96), new Vector2(0, 0));

        UI.Panel(header, "LineRed", colKenyaRed,
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 0), new Vector2(0, 8));
        UI.Panel(header, "LineGreen", colKenyaGreen,
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 8), new Vector2(0, 16));

        UI.Text(header, "HeaderTitle", headerTitle, 30, colKenyaWhite,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(28, 16), new Vector2(-28, -16),
            TextAlignmentOptions.MidlineLeft);

        var pill = UI.Panel(header, "MissionPill", new Color32(255, 255, 255, 34),
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-420, -22), new Vector2(-28, 22));
        UI.AddSoftOutline(pill.gameObject, new Color32(255, 255, 255, 50), 1);

        UI.Text(pill, "MissionText", missionName, 14, colKenyaWhite,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(14, 6), new Vector2(-14, -6),
            TextAlignmentOptions.Center);

        // Content (keeps clear spacing; no overlap)
        var content = UI.Panel(panelRT, "Content", new Color32(0, 0, 0, 0),
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(28, 110), new Vector2(-28, -110));

        // Step title
        var titleWrap = UI.Panel(content, "TitleWrap", new Color32(0, 0, 0, 0),
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -56), new Vector2(0, 0));

        stepTitleText = UI.Text(titleWrap, "StepTitle", "Welcome", 24, colText,
            Vector2.zero, Vector2.one, new Vector2(0, 10), new Vector2(0, -10),
            TextAlignmentOptions.MidlineLeft);
        stepTitleText.enableWordWrapping = false;

        // Body card (pattern clipped behind text)
        var bodyCard = UI.Panel(content, "BodyCard", new Color32(255, 255, 255, 245),
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(0, -66));
        UI.AddSoftOutline(bodyCard.gameObject, new Color32(0, 0, 0, 25), 1);

        var mask = bodyCard.gameObject.GetComponent<Mask>();
        if (!mask) mask = bodyCard.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // pattern root added later by EnsureKitengeExists / BuildKitenge

        stepBodyText = UI.Text(bodyCard, "StepBody", "", 18, colMuted,
            Vector2.zero, Vector2.one, new Vector2(18, 18), new Vector2(-18, -18),
            TextAlignmentOptions.TopLeft);
        stepBodyText.enableWordWrapping = true;

        // Footer
        var footer = UI.Panel(panelRT, "Footer", new Color32(240, 240, 240, 255),
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 0), new Vector2(0, 96));
        UI.AddSoftOutline(footer.gameObject, new Color32(0, 0, 0, 20), 1);

        pagerText = UI.Text(footer, "Pager", "1/7", 16, colMuted,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(28, -14), new Vector2(120, 14),
            TextAlignmentOptions.Center);

        backBtn = UI.MakeButton(footer, "BackBtn", "← Back", 18, colSecondary, colText,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-520, -24), new Vector2(-380, 24),
            () => Back());

        nextBtn = UI.MakeButton(footer, "NextBtn", "Next →", 18, colPrimary, colPrimaryText,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-360, -24), new Vector2(-220, 24),
            () => Next());

        startBtn = UI.MakeButton(footer, "StartBtn", "Start Mission →", 18, colPrimary, colPrimaryText,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-360, -24), new Vector2(-220, 24),
            () => Finish());

        skipBtn = UI.MakeButton(footer, "SkipBtn", "Skip", 18, colSecondary, colText,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-200, -24), new Vector2(-60, 24),
            () => Close());

        // Hide in edit mode by default (but keep in the scene)
        if (!Application.isPlaying)
            canvasGO.SetActive(false);

        // If in play mode, show as requested
        if (Application.isPlaying && showOnStart)
            canvasGO.SetActive(true);

        // Cache refs + ensure data
        CacheUIRefs(canvasGO.transform);

        if (Application.isPlaying)
        {
            EnsureEventSystemRuntime();
            WireButtonsRuntime();
            BuildSteps();
            UpdateNav();
        }
    }

    void CacheUIRefs(Transform existing)
    {
        canvas = existing.GetComponentInChildren<Canvas>(true);
        panelRT = existing.Find("Root/Panel") as RectTransform;

        stepTitleText = existing.Find("Root/Panel/Content/TitleWrap/StepTitle")?.GetComponent<TMP_Text>();
        stepBodyText  = existing.Find("Root/Panel/Content/BodyCard/StepBody")?.GetComponent<TMP_Text>();
        pagerText     = existing.Find("Root/Panel/Footer/Pager")?.GetComponent<TMP_Text>();

        backBtn  = existing.Find("Root/Panel/Footer/BackBtn")?.GetComponent<Button>();
        nextBtn  = existing.Find("Root/Panel/Footer/NextBtn")?.GetComponent<Button>();
        startBtn = existing.Find("Root/Panel/Footer/StartBtn")?.GetComponent<Button>();
        skipBtn  = existing.Find("Root/Panel/Footer/SkipBtn")?.GetComponent<Button>();
    }

    void EnsureKitengeExists()
    {
        if (!enableKitengePattern) return;
        if (!panelRT) return;

        var bodyCard = panelRT.Find("Content/BodyCard") as RectTransform;
        if (!bodyCard) return;

        var existing = bodyCard.Find("KitengePattern");
        if (existing) return;

        BuildKitenge(bodyCard);
    }

    void BuildKitenge(RectTransform bodyCard)
    {
        // pattern root behind text
        var patternRoot = UI.Panel(bodyCard, "KitengePattern", new Color32(0, 0, 0, 0),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        patternRoot.GetComponent<Image>().raycastTarget = false;
        patternRoot.SetAsFirstSibling();

        // Make it visible enough (no harsh squares)
        Color a = colKenyaGreen; a.a = Mathf.Clamp(patternOpacity + 0.08f, 0.12f, 0.28f);
        Color b = colKenyaRed;   b.a = Mathf.Clamp(patternOpacity + 0.08f, 0.12f, 0.28f);
        Color c = colKenyaBlack; c.a = Mathf.Clamp(patternOpacity + 0.06f, 0.10f, 0.22f);

        int rows = 7;
        int cols = 11;

        float tile = patternTileSize;

        for (int r = 0; r < rows; r++)
        {
            for (int k = 0; k < cols; k++)
            {
                Color col = ((r + k) % 3 == 0) ? a : (((r + k) % 3 == 1) ? b : c);

                float x = 36 + k * patternSpacingX + (r % 2 == 0 ? 0 : patternSpacingX * 0.5f);
                float y = 36 + r * patternSpacingY;

                var dot = UI.Panel(patternRoot, $"Dot_{r}_{k}", col,
                    new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(x, -y), new Vector2(x + tile, -y - tile));

                var img = dot.GetComponent<Image>();
                img.raycastTarget = false;

                // soften: a diamond + tiny inner cut for kitenge feel
                dot.localRotation = Quaternion.Euler(0, 0, 45f);

                // inner
                var inner = UI.Panel(dot, "Inner", new Color32(255, 255, 255, 45),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-tile * 0.18f, -tile * 0.18f), new Vector2(tile * 0.18f, tile * 0.18f));
                inner.GetComponent<Image>().raycastTarget = false;
            }
        }
    }

    // =========================
    // Animations
    // =========================
    IEnumerator AnimateIn()
    {
        var cg = EnsureCanvasGroup(panelRT.gameObject);
        float t = 0f;
        float d = Mathf.Max(0.05f, animateDuration);

        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d);
            cg.alpha = k;
            panelRT.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, k);
            yield return null;
        }

        cg.alpha = 1f;
        panelRT.localScale = Vector3.one;
    }

    IEnumerator Typewriter(TMP_Text target, string full, float cps)
    {
        if (!target) yield break;

        float delay = 1f / Mathf.Max(10f, cps);
        target.text = "";

        for (int i = 0; i < full.Length; i++)
        {
            target.text += full[i];
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    void SetCanvasGroupAlpha(GameObject go, float a)
    {
        var cg = EnsureCanvasGroup(go);
        cg.alpha = a;
    }

    CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (!cg) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    // =========================
    // Cursor
    // =========================
    void SaveAndForceCursor()
    {
        prevLock = Cursor.lockState;
        prevVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void RestoreCursor()
    {
        Cursor.lockState = prevLock;
        Cursor.visible = prevVisible;
    }

    // =========================
    // EventSystem (works for both input systems)
    // =========================
    void EnsureEventSystemRuntime()
    {
        if (!Application.isPlaying) return;
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem", typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    // =========================
    // UI helper
    // =========================
    private static class UI
    {
        public static RectTransform MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Panel(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = go.GetComponent<Image>();
            img.color = color;
            return rt;
        }

        public static TMP_Text Text(Transform parent, string name, string text, int fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.enableWordWrapping = false;
            return t;
        }

        public static Button MakeButton(Transform parent, string name, string label, int fontSize,
            Color bg, Color fg,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            go.GetComponent<Image>().color = bg;

            var btn = go.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txt = Text(go.transform, "Label", label, fontSize, fg,
                Vector2.zero, Vector2.one, new Vector2(10, 6), new Vector2(-10, -6),
                TextAlignmentOptions.Center);
            txt.enableWordWrapping = false;

            return btn;
        }

        public static void AddSoftOutline(GameObject go, Color color, int distance)
        {
            var outline = go.GetComponent<Outline>();
            if (!outline) outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }
    }

    // =========================
    // Editor convenience
    // =========================
    public void RebuildNow()
    {
#if UNITY_EDITOR
        var existing = transform.Find(CanvasName);
        if (existing)
            Undo.DestroyObjectImmediate(existing.gameObject);
#endif
        BuildOrFindPersistentUI(editModePersistent: !Application.isPlaying);
        EnsureKitengeExists();
#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
#endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PhishingMissionBriefingUI))]
public class PhishingMissionBriefingUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        var t = (PhishingMissionBriefingUI)target;

        if (GUILayout.Button("Build / Rebuild Persistent Briefing UI"))
        {
            t.RebuildNow();
        }

        if (GUILayout.Button("Show Briefing (Edit Mode Preview)"))
        {
            var c = t.transform.Find("__PhishingBriefingCanvas_PERSISTENT");
            if (c) c.gameObject.SetActive(true);
        }

        if (GUILayout.Button("Hide Briefing"))
        {
            var c = t.transform.Find("__PhishingBriefingCanvas_PERSISTENT");
            if (c) c.gameObject.SetActive(false);
        }
    }
}
#endif
