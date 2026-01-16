using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Permanent monitor UI (Windows-like) built in EDIT MODE and PLAY MODE.
/// - Uses [ExecuteAlways] so it exists even after you stop Play.
/// - Rebuilds only when missing (or when you click Rebuild in inspector).
/// Attach to an empty GameObject near/parented to the Monitor.
/// </summary>
[ExecuteAlways]
public class PhishingMissionMonitorUI : MonoBehaviour
{
    [Header("World-space placement")]
    public Transform targetSurface;              // monitor plane transform
    public Vector2 canvasSizeMeters = new Vector2(0.62f, 0.36f);
    public float surfaceOffset = 0.002f;
    [Range(200, 2500)] public int pixelsPerUnit = 1200;
    public Camera uiCamera;

    [Header("Build")]
    public bool autoRebuild = true;
    public string rootName = "MonitorUI_ROOT";

    [Header("Window layout (DO NOT change if you already positioned your UI)")]
    public Vector2 accountWindowPos = new Vector2(120, -80);
    public Vector2 accountWindowSize = new Vector2(900, 620);

    public Vector2 dashboardWindowPos = new Vector2(120, -80);
    public Vector2 dashboardWindowSize = new Vector2(1060, 680);

    public Vector2 mailWindowPos = new Vector2(70, -60);
    public Vector2 mailWindowSize = new Vector2(1060, 680);

    public Vector2 browserWindowPos = new Vector2(120, -90);
    public Vector2 browserWindowSize = new Vector2(1040, 660);

    public Vector2 resultWindowPos = new Vector2(220, -140);
    public Vector2 resultWindowSize = new Vector2(860, 520);

    [Header("Mission tuning (baseline risk if you never click link)")]
    [Range(0, 100)] public int hackChanceIfWeak = 80;
    [Range(0, 100)] public int hackChanceIfMedium = 35;
    [Range(0, 100)] public int hackChanceIfStrong = 8;

    [Header("Mission tuning (risk if you click link + submit password)")]
    [Range(0, 100)] public int phishHackChanceWeak = 95;
    [Range(0, 100)] public int phishHackChanceMedium = 70;
    [Range(0, 100)] public int phishHackChanceStrong = 25;

    [Header("Realism delays")]
    [Range(0f, 5f)] public float browserLoadSeconds = 1.2f;
    [Range(0f, 5f)] public float submitLoadSeconds = 1.0f;
    [Range(0f, 5f)] public float postSuccessDelay = 1.2f;

    [Header("Hint glow")]
    public float hintPulseSpeed = 2.0f;
    public Color hintGlowColor = new Color32(0, 220, 255, 255);

    [Header("Suspicious domain highlight")]
    public string suspiciousDomain = "verify.kca-security.com";
    public string typosquatHint = "kca-security"; // the part to highlight
    public float domainFlashSpeed = 2.5f;

    // ---------- internal ----------
    private Canvas canvas;
    private RectTransform root;

    private enum MissionState { CreateAccount, Dashboard, Mail, Browser, Result }
    private MissionState state;

    private string chosenUsername = "student@cyberverse.local";
    private string chosenPassword = "";
    private PasswordGrade chosenGrade = PasswordGrade.Unknown;

    private bool openedMailOnce = false;
    private bool clickedVerify = false;
    private bool submittedOnPhishPage = false;

    private RectTransform desktop;

    private RectTransform accountWindow;
    private RectTransform dashboardWindow;
    private RectTransform mailWindow;
    private RectTransform browserWindow;
    private RectTransform resultWindow;

    // Account refs
    private TMP_InputField usernameInput;
    private TMP_InputField passwordInput;
    private TMP_Text strengthLabel;
    private TMP_Text strengthHint;
    private Image strengthBarFill;
    private TMP_Text accountError;

    // Dashboard refs
    private TMP_Text dashWelcome;
    private TMP_Text dashTaskText;

    // Mail refs
    private TMP_Text mailBodyText;
    private TMP_Text mailSenderLine;
    private TMP_Text mailSubjectLine;

    // Browser refs
    private TMP_Text browserStatus;
    private TMP_Text browserSecureLabel;
    private Image browserSecureDot;

    private RectTransform browserPageRoot;
    private RectTransform browserSuccessRoot;

    private TMP_InputField browserPassField;
    private TMP_Text browserError;
    private TMP_Text browserUrlText;

    // Buttons
    private Button btnContinue;
    private Button btnReset;

    private Button btnEmailIcon;
    private Button btnProfileIcon;
    private Button btnTipsIcon;

    private Button btnMailBack;
    private Button btnReport;
    private Button btnDelete;
    private Button btnVerifyButton;

    private Button btnBrowserSubmit;
    private Button btnBrowserBack;

    private Button btnResultRetry;
    private Button btnResultClose;

    // Hint target tracking
    private enum HintTarget { None, Continue, EmailIcon, VerifyButton, BrowserSubmit }
    private HintTarget currentHint = HintTarget.None;

    // Hint outlines for pulsing glow
    private readonly Dictionary<Button, Outline> hintOutlines = new();

    // palette (same family as your design)
    private readonly Color colDesktop = new Color32(14, 26, 44, 255);
    private readonly Color colWindow = new Color32(240, 243, 247, 255);
    private readonly Color colBorder = new Color32(210, 214, 220, 255);
    private readonly Color colText = new Color32(28, 30, 33, 255);
    private readonly Color colMuted = new Color32(98, 104, 112, 255);

    private readonly Color colDanger = new Color32(204, 46, 46, 255);
    private readonly Color colGood = new Color32(30, 148, 77, 255);
    private readonly Color colWarn = new Color32(227, 149, 0, 255);

    private readonly Color colPrimary = new Color32(19, 110, 192, 255);
    private readonly Color colLink = new Color32(11, 88, 171, 255);
    private readonly Color colLinkHover = new Color32(0, 130, 255, 255);

    private enum PasswordGrade { Unknown, Weak, Medium, Strong }

    void OnEnable()
    {
        EnsureEventSystem();
        EnsureUICamera();

        if (autoRebuild)
            BuildIfMissing();

        ApplyPlacement();
        CacheAndWire();
        RefreshHintState();
    }

    void OnValidate()
    {
        if (!enabled) return;

        if (autoRebuild)
            BuildIfMissing();

        ApplyPlacement();
        CacheAndWire();
    }

    void Update()
    {
        ApplyPlacement();

        // Only pulse in play mode
        if (Application.isPlaying)
        {
            PulseHintGlows();
            RefreshHintState();
            AnimateSuspiciousDomainFlash();
        }
    }

    // =========================
    // PUBLIC: Rebuild
    // =========================
    public void Rebuild()
    {
        DestroyRootIfExists();
        BuildAll();
        ApplyPlacement();
        CacheAndWire();
        RefreshHintState();
    }

    void BuildIfMissing()
    {
        var existing = transform.Find(rootName);
        if (!existing)
        {
            BuildAll();
        }
        else
        {
            CacheRefs(existing);
        }
    }

    void CacheRefs(Transform existingRoot)
    {
        root = existingRoot.GetComponent<RectTransform>();
        canvas = existingRoot.GetComponent<Canvas>();
        if (canvas) canvas.worldCamera = uiCamera;

        desktop = existingRoot.Find("Desktop")?.GetComponent<RectTransform>();
        if (!desktop) return;

        accountWindow = desktop.Find("AccountWindow")?.GetComponent<RectTransform>();
        dashboardWindow = desktop.Find("DashboardWindow")?.GetComponent<RectTransform>();
        mailWindow = desktop.Find("MailWindow")?.GetComponent<RectTransform>();
        browserWindow = desktop.Find("BrowserWindow")?.GetComponent<RectTransform>();
        resultWindow = desktop.Find("ResultWindow")?.GetComponent<RectTransform>();

        // Account
        if (accountWindow)
        {
            var body = accountWindow.Find("Body");
            usernameInput = body?.Find("UsernameField")?.GetComponent<TMP_InputField>();
            passwordInput = body?.Find("PasswordField")?.GetComponent<TMP_InputField>();
            accountError = body?.Find("Error")?.GetComponent<TMP_Text>();

            var strengthRow = body?.Find("StrengthRow");
            strengthLabel = strengthRow?.Find("StrengthLabel")?.GetComponent<TMP_Text>();
            strengthHint = strengthRow?.Find("StrengthHint")?.GetComponent<TMP_Text>();
            var barBg = strengthRow?.Find("StrengthBarBG");
            strengthBarFill = barBg?.Find("Fill")?.GetComponent<Image>();

            if (passwordInput != null)
            {
                passwordInput.onValueChanged.RemoveListener(OnPasswordChanged);
                passwordInput.onValueChanged.AddListener(OnPasswordChanged);
            }
        }

        // Dashboard
        if (dashboardWindow)
        {
            var body = dashboardWindow.Find("Body");
            dashWelcome = body?.Find("HeroCard/Welcome")?.GetComponent<TMP_Text>();
            dashTaskText = body?.Find("HeroCard/HintLine")?.GetComponent<TMP_Text>();
        }

        // Mail
        if (mailWindow)
        {
            var body = mailWindow.Find("Body");
            var right = body?.Find("RightPane");
            var scroll = right?.Find("MailScroll");
            var content = scroll?.Find("Content");
            mailBodyText = content?.Find("MailBody")?.GetComponent<TMP_Text>();

            mailSenderLine = content?.Find("Header/Sender")?.GetComponent<TMP_Text>();
            mailSubjectLine = content?.Find("Header/Subject")?.GetComponent<TMP_Text>();
        }

        // Browser
        if (browserWindow)
        {
            var body = browserWindow.Find("Body");
            browserStatus = body?.Find("TopBar/Status")?.GetComponent<TMP_Text>();
            browserSecureLabel = body?.Find("TopBar/SecureLabel")?.GetComponent<TMP_Text>();
            browserSecureDot = body?.Find("TopBar/SecureDot")?.GetComponent<Image>();
            browserUrlText = body?.Find("TopBar/UrlText")?.GetComponent<TMP_Text>();

            browserPageRoot = body?.Find("Page")?.GetComponent<RectTransform>();
            browserSuccessRoot = body?.Find("Success")?.GetComponent<RectTransform>();

            browserPassField = browserPageRoot?.Find("PasswordField")?.GetComponent<TMP_InputField>();
            browserError = browserPageRoot?.Find("Error")?.GetComponent<TMP_Text>();
        }
    }

    // =========================
    // Placement (unchanged)
    // =========================
    void ApplyPlacement()
    {
        if (!root || !targetSurface) return;

        root.position = targetSurface.position + targetSurface.forward * surfaceOffset;
        root.rotation = targetSurface.rotation;
        root.sizeDelta = new Vector2(canvasSizeMeters.x * pixelsPerUnit, canvasSizeMeters.y * pixelsPerUnit);

        if (canvas) canvas.worldCamera = uiCamera;
    }

    // =========================
    // Build
    // =========================
    void BuildAll()
    {
        BuildCanvas();
        BuildDesktop();

        BuildAccountWindow();
        BuildDashboardWindow();
        BuildMailWindow();
        BuildBrowserWindow();
        BuildResultWindow();

        SwitchState(MissionState.CreateAccount);
    }

    void BuildCanvas()
    {
        var go = new GameObject(rootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);

        root = go.GetComponent<RectTransform>();
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = uiCamera;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = pixelsPerUnit;
    }

    void BuildDesktop()
    {
        desktop = UI.Panel(root, "Desktop", colDesktop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UI.Panel(desktop, "DesktopOverlay", new Color32(255, 255, 255, 16), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    void BuildAccountWindow()
    {
        accountWindow = UI.Window(desktop, "AccountWindow", "Account Setup",
            accountWindowPos, accountWindowSize, colWindow, colBorder, colText,
            onClose: () => { });

        var body = accountWindow.Find("Body").GetComponent<RectTransform>();

        UI.Text(body, "Title", "Create your in-game account", 30, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -26), new Vector2(-28, -80),
            TextAlignmentOptions.TopLeft);

        UI.Text(body, "Sub", "Use a strong password. Weak passwords can be cracked.", 18, colMuted,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -78), new Vector2(-28, -116),
            TextAlignmentOptions.TopLeft);

        UI.Text(body, "UserLabel", "Username", 18, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -140), new Vector2(-28, -175),
            TextAlignmentOptions.TopLeft);

        usernameInput = UI.Input(body, "UsernameField", "student@cyberverse.local", 18,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -180), new Vector2(-28, -240));

        UI.Text(body, "PassLabel", "Password", 18, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -260), new Vector2(-28, -295),
            TextAlignmentOptions.TopLeft);

        passwordInput = UI.Input(body, "PasswordField", "Enter password…", 18,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -300), new Vector2(-28, -360),
            isPassword: true);

        var strengthRow = UI.Panel(body, "StrengthRow", new Color32(0, 0, 0, 0),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -372), new Vector2(-28, -430));

        strengthLabel = UI.Text(strengthRow, "StrengthLabel", "Strength: —", 18, colMuted,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(260, 0),
            TextAlignmentOptions.MidlineLeft);

        var barBg = UI.Panel(strengthRow, "StrengthBarBG", new Color32(225, 228, 234, 255),
            new Vector2(0, 0.25f), new Vector2(1, 0.75f), new Vector2(260, 8), new Vector2(-120, -8));
        barBg.GetComponent<Image>().raycastTarget = false;

        strengthBarFill = UI.Image(barBg, "Fill", colWarn,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 0));
        strengthBarFill.raycastTarget = false;

        strengthHint = UI.Text(strengthRow, "StrengthHint", "Tip: 12+ chars, mix UPPER/lower/numbers/symbols", 16, colMuted,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -28), new Vector2(0, 0),
            TextAlignmentOptions.TopLeft);

        accountError = UI.Text(body, "Error", "", 16, colDanger,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -432), new Vector2(-28, -470),
            TextAlignmentOptions.TopLeft);
        accountError.gameObject.SetActive(false);

        passwordInput.onValueChanged.RemoveListener(OnPasswordChanged);
        passwordInput.onValueChanged.AddListener(OnPasswordChanged);

        UI.Button(body, "ContinueBtn", "Continue →", 20, colPrimary, Color.white,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-260, 32), new Vector2(-28, 88),
            () => { });

        UI.Button(body, "ResetBtn", "Reset", 20, new Color32(235, 237, 241, 255), colText,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-390, 32), new Vector2(-270, 88),
            () => { });
    }

    void BuildDashboardWindow()
    {
        dashboardWindow = UI.Window(desktop, "DashboardWindow", "Dashboard",
            dashboardWindowPos, dashboardWindowSize, colWindow, colBorder, colText,
            onClose: () => { });

        var body = dashboardWindow.Find("Body").GetComponent<RectTransform>();

        var hero = UI.Panel(body, "HeroCard", Color.white,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(18, -18), new Vector2(-18, -190));
        UI.AddSoftOutline(hero.gameObject, new Color32(226, 230, 236, 255), 2);

        dashWelcome = UI.Text(hero, "Welcome", "Welcome.", 34, colText,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(22, -20), new Vector2(-22, -70),
            TextAlignmentOptions.TopLeft);

        dashTaskText = UI.Text(hero, "HintLine", "Task: Open Email to check a new message.", 18, colMuted,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(22, -66), new Vector2(-22, -110),
            TextAlignmentOptions.TopLeft);

        // icons area (same window, no new layout changes)
        var iconRow = UI.Panel(body, "IconRow", new Color32(0, 0, 0, 0),
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(18, -210), new Vector2(-18, -350));

        UI.IconButton(iconRow, "EmailIcon", "✉", "Email", "1 new message", new Color32(0, 140, 255, 255), () => { });
        UI.IconButton(iconRow, "ProfileIcon", "👤", "Profile", "Edit account", new Color32(255, 120, 40, 255), () => { });
        UI.IconButton(iconRow, "TipsIcon", "🛡", "Tips", "Check list", new Color32(120, 90, 255, 255), () => { });

        // simple manual placement (keeps within your existing window)
        PlaceIcon(iconRow, "EmailIcon", 0);
        PlaceIcon(iconRow, "ProfileIcon", 360);
        PlaceIcon(iconRow, "TipsIcon", 720);

        UpdateDashboardWelcome();
    }

    void PlaceIcon(RectTransform parent, string name, float x)
    {
        var rt = parent.Find(name)?.GetComponent<RectTransform>();
        if (!rt) return;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, 0);
        rt.sizeDelta = new Vector2(320, 130);
    }

    void BuildMailWindow()
    {
        mailWindow = UI.Window(desktop, "MailWindow", "Mail",
            mailWindowPos, mailWindowSize, colWindow, colBorder, colText,
            onClose: () => SwitchState(MissionState.Dashboard));

        var body = mailWindow.Find("Body").GetComponent<RectTransform>();

        var left = UI.Panel(body, "LeftPane", new Color32(247, 248, 250, 255),
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(300, 0));
        UI.Image(left, "RightBorder", colBorder, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-1, 0), new Vector2(0, 0));

        UI.Text(left, "Folders", "Inbox", 22, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -18), new Vector2(-18, -60),
            TextAlignmentOptions.TopLeft);

        // mail list item (for realism)
        var mailItem = UI.Button(left, "MailItem", "", 18, new Color32(235, 242, 255, 255), colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -70), new Vector2(-14, -158),
            () => { });

        var itemRT = mailItem.GetComponent<RectTransform>();
        UI.Text(itemRT, "From", "From: IT Support <it-support@kca-security.co.ke>", 15, colMuted,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -10), new Vector2(-14, -36),
            TextAlignmentOptions.TopLeft);
        UI.Text(itemRT, "Subject", "URGENT: Account verification required", 18, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -34), new Vector2(-14, -70),
            TextAlignmentOptions.TopLeft);
        UI.Text(itemRT, "Snippet", "We detected unusual activity. Verify within 30 minutes…", 15, colMuted,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -70), new Vector2(-14, -106),
            TextAlignmentOptions.TopLeft);

        var right = UI.Panel(body, "RightPane", Color.clear,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(300, 0), new Vector2(0, 0));

        var ribbon = UI.Panel(right, "Ribbon", new Color32(252, 252, 253, 255),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -74));
        UI.Image(ribbon, "BottomBorder", colBorder, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1));

        UI.Button(ribbon, "BackBtn", "← Dashboard", 18, new Color32(235, 237, 241, 255), colText,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(16, 14), new Vector2(170, -14),
            () => { });

        UI.Button(ribbon, "ReportBtn", "Report phishing", 18, new Color32(235, 237, 241, 255), colText,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(188, 14), new Vector2(360, -14),
            () => { });

        UI.Button(ribbon, "DeleteBtn", "Delete", 18, new Color32(235, 237, 241, 255), colText,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(376, 14), new Vector2(480, -14),
            () => { });

        var scroll = UI.ScrollView(right, "MailScroll",
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(18, 18), new Vector2(-18, -90));

        // Header section (so the verify button is at the START of email like you asked)
        var header = UI.Panel(scroll.content, "Header", new Color32(0, 0, 0, 0),
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(0, 0));
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 110;

        mailSubjectLine = UI.Text(header, "Subject", "Subject: URGENT: Account verification required", 18, colText,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -4), new Vector2(0, -34),
            TextAlignmentOptions.TopLeft);
        mailSubjectLine.enableWordWrapping = true;

        mailSenderLine = UI.Text(header, "Sender", "From: IT Support <it-support@kca-security.co.ke>", 16, colMuted,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -34), new Vector2(0, -62),
            TextAlignmentOptions.TopLeft);
        mailSenderLine.enableWordWrapping = true;

        // Verify button (ACTUAL button, not label)
        var verifyGO = UI.LayoutButtonFullWidth(scroll.content, "VerifyDomainBtn",
            $"VERIFY NOW  →  {suspiciousDomain}",
            18, new Color32(235, 242, 255, 255), colLink,
            height: 56,
            () => { });

        // Make it feel like a link (underline + hover)
        var verifyLabel = verifyGO.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (verifyLabel)
        {
            verifyLabel.fontStyle = FontStyles.Underline;
            verifyLabel.richText = true;
            UI.AddHoverTextColor(verifyGO, verifyLabel, colLink, colLinkHover);
        }
        UI.AddHoverScale(verifyGO, 1.00f, 1.03f);

        // Body text (below verify)
        mailBodyText = UI.LayoutText(scroll.content, "MailBody", BuildEmailBody(), 18, colText, TextAlignmentOptions.TopLeft);
    }

    void BuildBrowserWindow()
    {
        browserWindow = UI.Window(desktop, "BrowserWindow", "Browser",
            browserWindowPos, browserWindowSize, colWindow, colBorder, colText,
            onClose: () => SwitchState(MissionState.Mail));

        var body = browserWindow.Find("Body").GetComponent<RectTransform>();

        // Top bar
        var top = UI.Panel(body, "TopBar", new Color32(252, 252, 253, 255),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72), new Vector2(0, 0));
        UI.Image(top, "BottomBorder", colBorder,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1));

        UI.Button(top, "BackBtn", "←", 18, new Color32(235, 237, 241, 255), colText,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(12, 12), new Vector2(60, -12),
            () => { });

        // Secure indicator (NEXT UPGRADE)
        browserSecureDot = UI.Image(top, "SecureDot", colDanger,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(72, -8), new Vector2(88, 8));

        browserSecureLabel = UI.Text(top, "SecureLabel", "Not secure", 16, colDanger,
            new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(92, 12), new Vector2(190, -12),
            TextAlignmentOptions.MidlineLeft);

        browserUrlText = UI.Text(top, "UrlText", $"https://{suspiciousDomain}", 16, colText,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(200, 12), new Vector2(-240, -12),
            TextAlignmentOptions.MidlineLeft);

        browserStatus = UI.Text(top, "Status", "", 16, colMuted,
            new Vector2(1, 0), new Vector2(1, 1),
            new Vector2(-240, 12), new Vector2(-16, -12),
            TextAlignmentOptions.MidlineRight);

        // Phish Page
        browserPageRoot = UI.Panel(body, "Page", Color.white,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(18, 18), new Vector2(-18, -90));
        UI.AddSoftOutline(browserPageRoot.gameObject, new Color32(226, 230, 236, 255), 2);

        UI.Text(browserPageRoot, "PageTitle", "KCA Account Verification", 30, colText,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(26, -24), new Vector2(-26, -80),
            TextAlignmentOptions.TopLeft);

        UI.Text(browserPageRoot, "PageSub",
            "For security, please confirm your password to continue.",
            18, colMuted,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(26, -78), new Vector2(-26, -120),
            TextAlignmentOptions.TopLeft);

        UI.Text(browserPageRoot, "UserLabel", "Account", 18, colText,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(26, -150), new Vector2(-26, -185),
            TextAlignmentOptions.TopLeft);

        UI.Text(browserPageRoot, "UserValue", "", 18, colText,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(26, -185), new Vector2(-26, -220),
            TextAlignmentOptions.TopLeft);

        UI.Text(browserPageRoot, "PassLabel", "Password", 18, colText,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(26, -250), new Vector2(-26, -285),
            TextAlignmentOptions.TopLeft);

        browserPassField = UI.Input(browserPageRoot, "PasswordField", "Enter your password…", 18,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(26, -292), new Vector2(-26, -356),
            isPassword: true);

        browserError = UI.Text(browserPageRoot, "Error", "", 16, colDanger,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(26, -362), new Vector2(-26, -398),
            TextAlignmentOptions.TopLeft);
        browserError.gameObject.SetActive(false);

        var submitGO = UI.Button(browserPageRoot, "SubmitBtn", "Sign in", 20, colPrimary, Color.white,
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-240, -420), new Vector2(-26, -372),
            () => { });
        UI.AddHoverScale(submitGO, 1.00f, 1.03f);

        UI.Text(browserPageRoot, "Footer", "© KCA Security • Help • Privacy • Terms", 14, colMuted,
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(26, 22), new Vector2(-26, 54),
            TextAlignmentOptions.MidlineLeft);

        // Success page (NEXT UPGRADE): shows even if hacked (silent compromise)
        browserSuccessRoot = UI.Panel(body, "Success", Color.white,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(18, 18), new Vector2(-18, -90));
        UI.AddSoftOutline(browserSuccessRoot.gameObject, new Color32(226, 230, 236, 255), 2);

        UI.Text(browserSuccessRoot, "SuccessTitle", "Verification successful", 34, colGood,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(26, -36), new Vector2(-26, -110),
            TextAlignmentOptions.TopLeft);

        UI.Text(browserSuccessRoot, "SuccessBody",
            "Thank you. Your account has been verified.\n\nYou may close this page.",
            18, colText,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(26, -110), new Vector2(-26, -190),
            TextAlignmentOptions.TopLeft);

        UI.Text(browserSuccessRoot, "SmallNote",
            "Tip: Legit organizations rarely ask you to enter your password from an email link.",
            16, colMuted,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(26, -190), new Vector2(-26, -240),
            TextAlignmentOptions.TopLeft);

        browserSuccessRoot.gameObject.SetActive(false);
    }

    void BuildResultWindow()
    {
        resultWindow = UI.Window(desktop, "ResultWindow", "Security Outcome",
            resultWindowPos, resultWindowSize, colWindow, colBorder, colText,
            onClose: () => ShowWindow(resultWindow, false));

        var body = resultWindow.Find("Body").GetComponent<RectTransform>();

        UI.Text(body, "OutcomeTitle", "—", 34, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -26), new Vector2(-26, -88),
            TextAlignmentOptions.TopLeft);

        UI.Text(body, "OutcomeBody", "—", 18, colText,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(26, 120), new Vector2(-26, -92),
            TextAlignmentOptions.TopLeft);

        UI.Button(body, "RetryBtn", "Retry mission", 20, colPrimary, Color.white,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-240, 26), new Vector2(-26, 86),
            () => { });

        UI.Button(body, "CloseBtn", "Close", 20, new Color32(235, 237, 241, 255), colText,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-380, 26), new Vector2(-260, 86),
            () => { });
    }

    // =========================
    // State / Flow
    // =========================
    void SwitchState(MissionState newState)
    {
        state = newState;

        ShowWindow(accountWindow, state == MissionState.CreateAccount);
        ShowWindow(dashboardWindow, state == MissionState.Dashboard);
        ShowWindow(mailWindow, state == MissionState.Mail);
        ShowWindow(browserWindow, state == MissionState.Browser);
        ShowWindow(resultWindow, state == MissionState.Result);

        if (state == MissionState.Dashboard) UpdateDashboardWelcome();

        RefreshHintState();
    }

    void UpdateDashboardWelcome()
    {
        if (!dashWelcome) return;

        dashWelcome.text = $"Welcome, {chosenUsername}.";

        if (dashTaskText)
        {
            dashTaskText.text = openedMailOnce
                ? "Task: Inspect the email. Report/delete OR verify (risky)."
                : "Task: Open Email to check a new message.";
        }
    }

    // =========================
    // Gameplay actions
    // =========================
    void OnClickContinue()
    {
        ClearAccountError();

        chosenUsername = usernameInput ? usernameInput.text.Trim() : chosenUsername;
        chosenPassword = passwordInput ? passwordInput.text : "";
        chosenGrade = GradePassword(chosenPassword);

        if (string.IsNullOrWhiteSpace(chosenUsername))
        {
            SetAccountError("Please enter a username.");
            return;
        }

        if (chosenGrade == PasswordGrade.Unknown)
        {
            SetAccountError("Please enter a password.");
            return;
        }

        openedMailOnce = false;
        clickedVerify = false;
        submittedOnPhishPage = false;

        UpdateDashboardWelcome();
        SwitchState(MissionState.Dashboard);
    }

    void OnClickVerify()
    {
        clickedVerify = true;
        SwitchState(MissionState.Browser);

        // Fill browser account label
        var userValue = browserPageRoot ? browserPageRoot.Find("UserValue")?.GetComponent<TMP_Text>() : null;
        if (userValue) userValue.text = chosenUsername;

        // Loading behavior
        if (browserStatus) browserStatus.text = "Loading…";
        if (browserPageRoot) browserPageRoot.gameObject.SetActive(false);
        if (browserSuccessRoot) browserSuccessRoot.gameObject.SetActive(false);

        // secure label always not secure (realistic)
        if (browserSecureDot) browserSecureDot.color = colDanger;
        if (browserSecureLabel)
        {
            browserSecureLabel.text = "Not secure";
            browserSecureLabel.color = colDanger;
        }

        if (Application.isPlaying)
            StartCoroutine(BrowserLoadRoutine());
        else
        {
            if (browserStatus) browserStatus.text = "Not secure";
            if (browserPageRoot) browserPageRoot.gameObject.SetActive(true);
        }
    }

    IEnumerator BrowserLoadRoutine()
    {
        yield return new WaitForSeconds(browserLoadSeconds);
        if (browserStatus) browserStatus.text = "Not secure";
        if (browserPageRoot) browserPageRoot.gameObject.SetActive(true);
        RefreshHintState();
    }

    void OnClickBrowserSubmit()
    {
        if (!browserPassField) return;

        var entered = browserPassField.text ?? "";
        if (string.IsNullOrWhiteSpace(entered))
        {
            SetBrowserError("Password is required.");
            return;
        }

        ClearBrowserError();
        submittedOnPhishPage = true;

        if (browserStatus) browserStatus.text = "Signing in…";

        if (Application.isPlaying)
            StartCoroutine(SubmitRoutine());
        else
            ShowPostSuccessThenOutcome();
    }

    IEnumerator SubmitRoutine()
    {
        yield return new WaitForSeconds(submitLoadSeconds);
        ShowPostSuccessThenOutcome();
    }

    void ShowPostSuccessThenOutcome()
    {
        // Success page shown ALWAYS (even if compromised) — realism upgrade
        if (browserPageRoot) browserPageRoot.gameObject.SetActive(false);
        if (browserSuccessRoot) browserSuccessRoot.gameObject.SetActive(true);
        if (browserStatus) browserStatus.text = "Complete";

        if (Application.isPlaying)
            StartCoroutine(PostSuccessDelayRoutine());
        else
            ResolveOutcome();
    }

    IEnumerator PostSuccessDelayRoutine()
    {
        yield return new WaitForSeconds(postSuccessDelay);
        ResolveOutcome();
    }

    void ResolveOutcome()
    {
        // If they clicked + submitted password -> phishing scenario
        if (clickedVerify && submittedOnPhishPage)
        {
            int roll = UnityEngine.Random.Range(0, 101);
            int chance = chosenGrade switch
            {
                PasswordGrade.Weak => phishHackChanceWeak,
                PasswordGrade.Medium => phishHackChanceMedium,
                PasswordGrade.Strong => phishHackChanceStrong,
                _ => 80
            };

            bool hacked = roll < chance;

            if (hacked)
            {
                ShowOutcome("ACCOUNT COMPROMISED", colDanger,
                    $"You entered your password on a suspicious site.\n\n" +
                    $"Attack succeeded (roll {roll} < {chance}%).\n\n" +
                    $"Password strength: {chosenGrade}\n\nLessons:\n" +
                    "• Never type passwords from email links\n" +
                    "• Check domain spelling (typosquatting)\n" +
                    "• Use MFA + password manager\n" +
                    "• Report suspicious emails");
            }
            else
            {
                ShowOutcome("PHISH ATTEMPT FAILED", colWarn,
                    $"You entered your password on a suspicious site.\n\n" +
                    $"Attack failed (roll {roll} ≥ {chance}%).\n\n" +
                    $"Password strength: {chosenGrade}\n\nLesson:\n" +
                    "• Strong passwords reduce risk, but clicking is still dangerous.\n" +
                    "• Best action: report/delete and verify via official channels.");
            }

            SwitchState(MissionState.Result);
            return;
        }

        // If they never clicked verify, simulate baseline hacking chance based on password strength
        int roll2 = UnityEngine.Random.Range(0, 101);
        int chance2 = chosenGrade switch
        {
            PasswordGrade.Weak => hackChanceIfWeak,
            PasswordGrade.Medium => hackChanceIfMedium,
            PasswordGrade.Strong => hackChanceIfStrong,
            _ => 50
        };

        bool hacked2 = roll2 < chance2;

        if (hacked2)
        {
            ShowOutcome("ACCOUNT AT RISK", colWarn,
                $"You avoided the phishing link, but your password was weak.\n\n" +
                $"Risk simulation: (roll {roll2} < {chance2}%).\n\n" +
                $"Password strength: {chosenGrade}\n\nFix:\n" +
                "• Use 12–16+ characters\n" +
                "• Avoid common words\n" +
                "• Add symbols + uniqueness\n" +
                "• Enable MFA");
        }
        else
        {
            ShowOutcome("SAFE", colGood,
                $"You avoided the phishing link and your password held up.\n\n" +
                $"Password strength: {chosenGrade}\n\nBest practice:\n" +
                "• Use a password manager\n" +
                "• Enable MFA\n" +
                "• Report suspicious messages");
        }

        SwitchState(MissionState.Result);
    }

    void ShowOutcome(string title, Color titleColor, string body)
    {
        var bodyRT = resultWindow.Find("Body").GetComponent<RectTransform>();
        var t = bodyRT.Find("OutcomeTitle").GetComponent<TMP_Text>();
        var b = bodyRT.Find("OutcomeBody").GetComponent<TMP_Text>();

        t.text = title;
        t.color = titleColor;

        b.text = body;
        b.color = colText;
    }

    void ShowWindow(RectTransform w, bool show)
    {
        if (w) w.gameObject.SetActive(show);
    }

    // =========================
    // Mail content + domain flashing highlight (NEXT UPGRADE)
    // =========================
    string BuildEmailBody()
    {
        return
            "Hello,\n\n" +
            "We detected unusual sign-in activity on your account.\n" +
            "To avoid suspension, verify your account within 30 minutes.\n\n" +
            "Warning signs:\n" +
            "• Creates panic/urgency\n" +
            "• Generic greeting\n" +
            "• Suspicious domain spelling\n" +
            "• Asks you to enter your password\n\n" +
            "If you weren’t expecting this message, report it as phishing.\n";
    }

    void AnimateSuspiciousDomainFlash()
    {
        // This only visually affects the Verify button label (link-like)
        if (!btnVerifyButton) return;

        var label = btnVerifyButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (!label) return;

        // Flash just the typosquat part
        float t = 0.5f + 0.5f * Mathf.Sin(Time.time * domainFlashSpeed);
        var flash = Color.Lerp(colWarn, colDanger, t);
        string flashHex = ColorUtility.ToHtmlStringRGB(flash);

        string domain = suspiciousDomain;
        if (!string.IsNullOrWhiteSpace(typosquatHint) && domain.Contains(typosquatHint))
        {
            domain = domain.Replace(typosquatHint, $"<color=#{flashHex}><b>{typosquatHint}</b></color>");
        }

        label.richText = true;
        label.text = $"VERIFY NOW  →  {domain}";
    }

    // =========================
    // Password UI
    // =========================
    void OnPasswordChanged(string value)
    {
        if (!strengthLabel || !strengthBarFill || !strengthHint) return;

        var grade = GradePassword(value);
        var (score, label, color) = PasswordToUI(grade, value);

        strengthLabel.text = $"Strength: {label}";
        strengthLabel.color = (grade == PasswordGrade.Strong) ? colGood : (grade == PasswordGrade.Medium ? colWarn : colDanger);

        float w = Mathf.Clamp01(score / 100f);
        strengthBarFill.rectTransform.anchorMax = new Vector2(w, 1);
        strengthBarFill.color = color;

        strengthHint.text =
            grade == PasswordGrade.Weak ? "Tip: Avoid common words. Add length + symbols." :
            grade == PasswordGrade.Medium ? "Tip: Add more length or a symbol for better safety." :
            grade == PasswordGrade.Strong ? "Nice! Strong password reduces hacking risk." :
            "Tip: 12+ chars, mix UPPER/lower/numbers/symbols";
    }

    PasswordGrade GradePassword(string p)
    {
        if (string.IsNullOrWhiteSpace(p)) return PasswordGrade.Unknown;

        int score = 0;
        if (p.Length >= 8) score += 20;
        if (p.Length >= 12) score += 25;
        if (p.Length >= 16) score += 10;

        if (Regex.IsMatch(p, "[a-z]")) score += 10;
        if (Regex.IsMatch(p, "[A-Z]")) score += 10;
        if (Regex.IsMatch(p, "[0-9]")) score += 10;
        if (Regex.IsMatch(p, @"[^a-zA-Z0-9]")) score += 15;

        if (Regex.IsMatch(p.ToLower(), @"password|qwerty|1234|admin|letmein|kenya|nairobi")) score -= 30;
        if (Regex.IsMatch(p, @"(.)\1\1")) score -= 10;
        if (Regex.IsMatch(p, @"12345|67890")) score -= 10;

        score = Mathf.Clamp(score, 0, 100);

        if (score < 45) return PasswordGrade.Weak;
        if (score < 75) return PasswordGrade.Medium;
        return PasswordGrade.Strong;
    }

    (int score, string label, Color color) PasswordToUI(PasswordGrade grade, string p)
    {
        int score = 0;
        if (!string.IsNullOrEmpty(p))
        {
            score = Mathf.Clamp(p.Length * 6, 0, 100);
            if (Regex.IsMatch(p, "[A-Z]")) score += 10;
            if (Regex.IsMatch(p, "[0-9]")) score += 10;
            if (Regex.IsMatch(p, @"[^a-zA-Z0-9]")) score += 10;
            score = Mathf.Clamp(score, 0, 100);
        }

        return grade switch
        {
            PasswordGrade.Weak => (Mathf.Min(score, 40), "Weak", colDanger),
            PasswordGrade.Medium => (Mathf.Clamp(score, 45, 75), "Medium", colWarn),
            PasswordGrade.Strong => (Mathf.Clamp(score, 75, 100), "Strong", colGood),
            _ => (0, "—", colMuted),
        };
    }

    // =========================
    // Buttons + hints wiring
    // =========================
    void CacheAndWire()
    {
        // Account buttons
        btnContinue = FindButton(accountWindow, "ContinueBtn");
        btnReset = FindButton(accountWindow, "ResetBtn");

        // Dashboard icon buttons
        btnEmailIcon = FindButton(dashboardWindow, "EmailIcon");
        btnProfileIcon = FindButton(dashboardWindow, "ProfileIcon");
        btnTipsIcon = FindButton(dashboardWindow, "TipsIcon");

        // Mail buttons
        btnMailBack = FindButton(mailWindow, "BackBtn");
        btnReport = FindButton(mailWindow, "ReportBtn");
        btnDelete = FindButton(mailWindow, "DeleteBtn");
        btnVerifyButton = FindButton(mailWindow, "VerifyDomainBtn");

        // Browser buttons
        btnBrowserBack = FindButton(browserWindow, "BackBtn");
        btnBrowserSubmit = FindButton(browserWindow, "SubmitBtn");

        // Result buttons
        btnResultRetry = FindButton(resultWindow, "RetryBtn");
        btnResultClose = FindButton(resultWindow, "CloseBtn");

        // Wire safely
        Wire(btnContinue, OnClickContinue);

        Wire(btnReset, () =>
        {
            if (usernameInput) usernameInput.text = "student@cyberverse.local";
            if (passwordInput) passwordInput.text = "";
            chosenUsername = "student@cyberverse.local";
            chosenPassword = "";
            chosenGrade = PasswordGrade.Unknown;

            openedMailOnce = false;
            clickedVerify = false;
            submittedOnPhishPage = false;

            ClearAccountError();
            ClearBrowserError();

            SwitchState(MissionState.CreateAccount);
        });

        Wire(btnEmailIcon, () =>
        {
            openedMailOnce = true;
            SwitchState(MissionState.Mail);
        });

        Wire(btnProfileIcon, () => SwitchState(MissionState.CreateAccount));

        Wire(btnTipsIcon, () =>
        {
            ShowOutcome("SECURITY TIPS", colPrimary,
                "Checklist:\n\n• Check sender address\n• Inspect domain spelling\n• Beware urgency/threats\n• Never type password from email links\n• Use MFA + password manager\n• Report suspicious emails");
            SwitchState(MissionState.Result);
        });

        Wire(btnMailBack, () => SwitchState(MissionState.Dashboard));

        Wire(btnReport, () =>
        {
            ShowOutcome("SAFE ACTION", colGood,
                "You reported the email. Great.\n\nLesson:\n• Reporting helps protect others\n• Verify through official channels, not email links.");
            SwitchState(MissionState.Result);
        });

        Wire(btnDelete, () =>
        {
            ShowOutcome("SAFE ACTION", colGood,
                "You deleted the suspicious email.\n\nLesson:\n• Deleting reduces risk\n• If unsure, contact IT using official contacts.");
            SwitchState(MissionState.Result);
        });

        Wire(btnVerifyButton, OnClickVerify);
        Wire(btnBrowserBack, () => SwitchState(MissionState.Mail));
        Wire(btnBrowserSubmit, OnClickBrowserSubmit);

        Wire(btnResultRetry, () =>
        {
            openedMailOnce = false;
            clickedVerify = false;
            submittedOnPhishPage = false;

            if (passwordInput) passwordInput.text = "";
            if (browserPassField) browserPassField.text = "";

            chosenPassword = "";
            chosenGrade = PasswordGrade.Unknown;

            ClearAccountError();
            ClearBrowserError();

            SwitchState(MissionState.CreateAccount);
        });

        Wire(btnResultClose, () => SwitchState(MissionState.Dashboard));

        // Hint outlines
        hintOutlines.Clear();
        RegisterHintOutline(btnContinue);
        RegisterHintOutline(btnEmailIcon);
        RegisterHintOutline(btnVerifyButton);
        RegisterHintOutline(btnBrowserSubmit);

        // Update dashboard welcome after wiring
        UpdateDashboardWelcome();
    }

    void RefreshHintState()
    {
        if (!Application.isPlaying) return;

        if (state == MissionState.CreateAccount) currentHint = HintTarget.Continue;
        else if (state == MissionState.Dashboard) currentHint = openedMailOnce ? HintTarget.None : HintTarget.EmailIcon;
        else if (state == MissionState.Mail) currentHint = HintTarget.VerifyButton;
        else if (state == MissionState.Browser) currentHint = HintTarget.BrowserSubmit;
        else currentHint = HintTarget.None;
    }

    void PulseHintGlows()
    {
        float a = 0.25f + 0.75f * Mathf.PingPong(Time.time * hintPulseSpeed, 1f);

        SetHintAlpha(btnContinue, currentHint == HintTarget.Continue ? a : 0f);
        SetHintAlpha(btnEmailIcon, currentHint == HintTarget.EmailIcon ? a : 0f);
        SetHintAlpha(btnVerifyButton, currentHint == HintTarget.VerifyButton ? a : 0f);
        SetHintAlpha(btnBrowserSubmit, currentHint == HintTarget.BrowserSubmit ? a : 0f);
    }

    void RegisterHintOutline(Button b)
    {
        if (!b) return;

        var img = b.GetComponent<Image>();
        if (!img) return;

        var o = b.GetComponent<Outline>();
        if (!o) o = b.gameObject.AddComponent<Outline>();

        o.effectDistance = new Vector2(4, -4);
        var c = hintGlowColor; c.a = 0f;
        o.effectColor = c;

        hintOutlines[b] = o;
    }

    void SetHintAlpha(Button b, float alpha)
    {
        if (!b) return;
        if (!hintOutlines.TryGetValue(b, out var o) || !o) return;

        var c = hintGlowColor;
        c.a = Mathf.Clamp01(alpha);
        o.effectColor = c;
    }

    static void Wire(Button b, Action action)
    {
        if (!b) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(() => action?.Invoke());
    }

    static Button FindButton(RectTransform window, string name)
    {
        if (!window) return null;
        var t = FindDeep(window, name);
        return t ? t.GetComponent<Button>() : null;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (!root) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            var r = FindDeep(c, name);
            if (r) return r;
        }
        return null;
    }

    // =========================
    // Errors
    // =========================
    void SetAccountError(string msg)
    {
        if (!accountError) return;
        accountError.text = msg;
        accountError.gameObject.SetActive(true);
    }

    void ClearAccountError()
    {
        if (!accountError) return;
        accountError.text = "";
        accountError.gameObject.SetActive(false);
    }

    void SetBrowserError(string msg)
    {
        if (!browserError) return;
        browserError.text = msg;
        browserError.gameObject.SetActive(true);
    }

    void ClearBrowserError()
    {
        if (!browserError) return;
        browserError.text = "";
        browserError.gameObject.SetActive(false);
    }

    // =========================
    // Unity basics
    // =========================
    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    void EnsureUICamera()
    {
        if (!uiCamera) uiCamera = Camera.main;
    }

    void DestroyRootIfExists()
    {
        var existing = transform.Find(rootName);
        if (!existing) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(existing.gameObject);
        else
            Destroy(existing.gameObject);
#else
        Destroy(existing.gameObject);
#endif
    }

    // =========================
    // UI Helper Library
    // =========================
    private static class UI
    {
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

            go.GetComponent<Image>().color = color;
            return rt;
        }

        public static Image Image(Transform parent, string name, Color color,
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
            img.raycastTarget = false;
            return img;
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
            t.richText = true;
            return t;
        }

        public static GameObject Button(Transform parent, string name, string label, int fontSize,
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
            btn.onClick.AddListener(() => onClick?.Invoke());

            var t = Text(go.transform, "Label", label, fontSize, fg,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 6), new Vector2(-10, -6),
                TextAlignmentOptions.Center);
            t.enableWordWrapping = true;

            return go;
        }

        public static TMP_InputField Input(Transform parent, string name, string placeholder, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            bool isPassword = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            go.GetComponent<Image>().color = Color.white;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color32(210, 214, 220, 255);
            outline.effectDistance = new Vector2(1, -1);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRT = textGo.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.offsetMin = new Vector2(12, 8);
            textRT.offsetMax = new Vector2(-12, -10);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = new Color32(28, 30, 33, 255);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = false;

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(go.transform, false);
            var phRT = phGo.GetComponent<RectTransform>();
            phRT.anchorMin = new Vector2(0, 0);
            phRT.anchorMax = new Vector2(1, 1);
            phRT.offsetMin = new Vector2(12, 8);
            phRT.offsetMax = new Vector2(-12, -10);

            var ph = phGo.AddComponent<TextMeshProUGUI>();
            ph.text = placeholder;
            ph.fontSize = fontSize;
            ph.color = new Color32(130, 136, 145, 255);
            ph.alignment = TextAlignmentOptions.MidlineLeft;

            var input = go.AddComponent<TMP_InputField>();
            input.textComponent = text;
            input.placeholder = ph;
            input.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;

            return input;
        }

        public static RectTransform Window(Transform parent, string name, string title,
            Vector2 anchoredPos, Vector2 size,
            Color windowBg, Color border, Color textColor,
            Action onClose)
        {
            var frame = Panel(parent, name, windowBg,
                new Vector2(0, 1), new Vector2(0, 1),
                anchoredPos, anchoredPos + size);

            AddSoftOutline(frame.gameObject, border, 2);

            var titleBar = Panel(frame, "TitleBar", new Color32(252, 252, 253, 255),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -54), new Vector2(0, 0));

            Image(titleBar, "BottomBorder", border,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1));

            Text(titleBar, "Title", title, 20, textColor,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 6), new Vector2(-70, -6),
                TextAlignmentOptions.MidlineLeft);

            Button(titleBar, "Close", "✕", 18,
                new Color32(235, 237, 241, 255), textColor,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(-54, 8), new Vector2(-10, -8),
                onClose);

            Panel(frame, "Body", new Color32(0, 0, 0, 0),
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -54));

            return frame;
        }

        public struct ScrollRefs { public RectTransform root; public RectTransform content; }

        public static ScrollRefs ScrollView(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            root.transform.SetParent(parent, false);

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            root.GetComponent<Image>().color = Color.white;
            var mask = root.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(root.transform, false);
            var content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(18, -650);
            content.offsetMax = new Vector2(-18, -18);

            var fitter = contentGo.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = contentGo.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 14;

            var scroll = root.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;

            return new ScrollRefs { root = rt, content = content };
        }

        public static TMP_Text LayoutText(Transform parent, string name, string text, int fontSize, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0, 10);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;

            var le = go.GetComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = 10;

            return tmp;
        }

        public static GameObject LayoutButtonFullWidth(Transform parent, string name, string label,
            int fontSize, Color bg, Color fg, float height, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0, height);

            go.GetComponent<Image>().color = bg;

            var le = go.GetComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1;

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var text = new GameObject("Label", typeof(RectTransform));
            text.transform.SetParent(go.transform, false);
            var trt = text.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(14, 6);
            trt.offsetMax = new Vector2(-14, -6);

            var tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = fg;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            tmp.richText = true;

            return go;
        }

        public static GameObject IconButton(Transform parent, string name,
            string iconText, string title, string subtitle, Color iconBg, Action onClick)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);
            card.GetComponent<Image>().color = Color.white;

            AddSoftOutline(card, new Color32(226, 230, 236, 255), 2);

            var btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var icon = Panel(card.transform, "Icon", iconBg,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(18, -32), new Vector2(82, 32));

            var iconTxt = Text(icon, "IconText", iconText, 30, Color.white,
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(0, 0),
                TextAlignmentOptions.Center);
            iconTxt.raycastTarget = false;

            Text(card.transform, "Title", title, 22, new Color32(28, 30, 33, 255),
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(96, -18), new Vector2(-18, -58),
                TextAlignmentOptions.TopLeft);

            Text(card.transform, "Sub", subtitle, 16, new Color32(98, 104, 112, 255),
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(96, -54), new Vector2(-18, -90),
                TextAlignmentOptions.TopLeft);

            return card;
        }

        public static void AddSoftOutline(GameObject go, Color color, int distance)
        {
            var o = go.GetComponent<Outline>();
            if (!o) o = go.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(distance, -distance);
        }

        public static void AddHoverScale(GameObject go, float normalScale, float hoverScale)
        {
            if (!go) return;

            var tr = go.GetComponent<RectTransform>();
            if (!tr) return;

            var et = go.GetComponent<EventTrigger>();
            if (!et) et = go.AddComponent<EventTrigger>();
            et.triggers ??= new List<EventTrigger.Entry>();

            void Add(EventTriggerType type, Action act)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(_ => act());
                et.triggers.Add(entry);
            }

            Add(EventTriggerType.PointerEnter, () => tr.localScale = Vector3.one * hoverScale);
            Add(EventTriggerType.PointerExit, () => tr.localScale = Vector3.one * normalScale);
        }

        public static void AddHoverTextColor(GameObject go, TextMeshProUGUI label, Color normal, Color hover)
        {
            if (!go || !label) return;

            var et = go.GetComponent<EventTrigger>();
            if (!et) et = go.AddComponent<EventTrigger>();
            et.triggers ??= new List<EventTrigger.Entry>();

            void Add(EventTriggerType type, Action act)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(_ => act());
                et.triggers.Add(entry);
            }

            Add(EventTriggerType.PointerEnter, () => label.color = hover);
            Add(EventTriggerType.PointerExit, () => label.color = normal);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PhishingMissionMonitorUI))]
public class PhishingMissionMonitorUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var t = (PhishingMissionMonitorUI)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Rebuild UI (Permanent)"))
        {
            t.Rebuild();
        }
    }
}
#endif
