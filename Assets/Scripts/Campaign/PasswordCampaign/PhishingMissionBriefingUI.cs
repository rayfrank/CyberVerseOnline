using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// eCitizen Kenyanized phishing + password safety lesson UI (Permanent, world-space).
/// Flow:
/// Home -> Official Login -> Dashboard -> Mail (Inbox) -> (Report/Delete OR Click Verify) -> Browser -> Outcome -> Result.
/// Includes:
/// - hints for right/wrong
/// - reward score for good behavior (with HUD UI)
/// - BIG animated red alert overlay if hacked
/// - end screen explains WHAT YOU DID WRONG + WHAT TO DO NEXT
/// </summary>
[ExecuteAlways]
public class PhishingMissionMonitorUI : MonoBehaviour
{
    [Header("World-space placement")]
    public Transform targetSurface;
    public Vector2 canvasSizeMeters = new Vector2(0.70f, 0.40f);
    public float surfaceOffset = 0.002f;
    [Range(200, 2500)] public int pixelsPerUnit = 1200;
    public Camera uiCamera;

    [Header("Build")]
    public bool autoRebuild = true;
    public string rootName = "MonitorUI_ROOT";

    [Header("Layout")]
    public Vector2 homeWindowPos = new Vector2(40, -40);
    public Vector2 homeWindowSize = new Vector2(1200, 720);

    public Vector2 dashboardWindowPos = new Vector2(60, -60);
    public Vector2 dashboardWindowSize = new Vector2(1180, 700);

    public Vector2 mailWindowPos = new Vector2(70, -60);
    public Vector2 mailWindowSize = new Vector2(1180, 700);

    public Vector2 browserWindowPos = new Vector2(80, -80);
    public Vector2 browserWindowSize = new Vector2(1140, 690);

    public Vector2 resultWindowPos = new Vector2(220, -140);
    public Vector2 resultWindowSize = new Vector2(900, 560);

    [Header("Security Simulation (risk if you enter password on suspicious page)")]
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

    [Header("Domains (Kenyanized)")]
    public string officialDomain = "accounts.ecitizen.go.ke";
    public string suspiciousDomain = "ecitizen-verify.go-ke.com";
    public string typosquatHint = "go-ke";
    public float domainFlashSpeed = 2.5f;

    [Header("Hacked Alert (BIG & LOUD UI)")]
    public float hackedFlashSpeed = 3.0f; // red flash speed
    public Color hackedFlashColor = new Color32(220, 30, 30, 140);
    public float hackedShakeAmount = 6f;
    public float hackedShakeSpeed = 18f;
    public float hackedPulseSpeed = 2.2f;

    [Header("Reward HUD")]
    public bool showRewardHUD = true;
    public float toastDuration = 2.0f;

    // ---------- internal ----------
    private Canvas canvas;
    private RectTransform root;
    private RectTransform desktop;

    private enum AppState { Home, BrowserOfficialLogin, Dashboard, Mail, BrowserPhishVerify, Result }
    private AppState state;

    // “Account”
    private string citizenIdOrEmail = "citizen@ecitizen.local";
    private string chosenPassword = "";
    private PasswordGrade chosenGrade = PasswordGrade.Unknown;

    // Lesson flags
    private bool loggedIn = false;
    private bool openedInbox = false;
    private bool clickedSuspiciousVerify = false;
    private bool submittedOnPhishPage = false;
    private bool reportedEmail = false;
    private bool deletedEmail = false;
    private bool hacked = false;

    // Rewards
    private int score = 0;
    private string lastScoreReason = "";
    private int lastScoreDelta = 0;
    private float lastScoreTime = -999f;

    // WINDOWS
    private RectTransform homeWindow;
    private RectTransform dashboardWindow;
    private RectTransform mailWindow;
    private RectTransform browserWindow;
    private RectTransform resultWindow;

    // HACK overlay + BIG ALERT
    private RectTransform hackedOverlay;
    private Image hackedOverlayImage;

    private RectTransform hackedBigPanel;
    private TMP_Text hackedBigTitle;
    private TMP_Text hackedBigBody;
    private RectTransform hackedSirenLeft;
    private RectTransform hackedSirenRight;

    private Coroutine hackedFlashRoutine;

    // REWARD HUD (always visible)
    private RectTransform hudRoot;
    private TMP_Text hudScore;
    private TMP_Text hudBadge;
    private TMP_Text hudToast;
    private Image hudToastBg;

    // HOME refs
    private TMP_InputField homeSearchField;
    private TMP_Text homeWelcomeLine;
    private Button btnHomeSignIn;
    private Button btnHomeRegister;

    // DASH refs
    private TMP_Text dashTitle;
    private TMP_Text dashSub;
    private TMP_Text dashHint;
    private RectTransform dashAlertBanner;
    private TMP_Text dashAlertText;

    private Button btnDashOpenInbox;
    private Button btnDashLogout;
    private Button btnDashBackHome;

    // MAIL refs
    private TMP_Text mailTitleLine;
    private TMP_Text mailFromLine;
    private TMP_Text mailSubjectLine;
    private TMP_Text mailBodyText;
    private TMP_Text mailHintBox;

    private Button btnMailBack;
    private Button btnMailReport;
    private Button btnMailDelete;
    private Button btnMailVerify; // suspicious link
    private Button btnMailOpenItem;

    // BROWSER refs
    private TMP_Text browserStatus;
    private TMP_Text browserSecureLabel;
    private Image browserSecureDot;
    private TMP_Text browserUrlText;

    private RectTransform browserPageRoot;
    private RectTransform browserSuccessRoot;

    private TMP_InputField browserUserField;
    private TMP_InputField browserPassField;
    private TMP_Text browserError;
    private TMP_Text browserPageTitle;
    private TMP_Text browserPageSub;
    private TMP_Text browserHintBox;

    private Button btnBrowserBack;
    private Button btnBrowserSubmit;

    // RESULT refs
    private Button btnResultRetry;
    private Button btnResultClose;

    // Hint targets
    private enum HintTarget { None, HomeSignIn, DashInbox, MailSafeAction, MailVerify, BrowserSubmit }
    private HintTarget currentHint = HintTarget.None;

    private readonly Dictionary<Button, Outline> hintOutlines = new();

    // Palette
    private readonly Color colDesktop = new Color32(245, 247, 250, 255);
    private readonly Color colWindow = new Color32(255, 255, 255, 255);
    private readonly Color colBorder = new Color32(220, 225, 232, 255);
    private readonly Color colText = new Color32(24, 28, 33, 255);
    private readonly Color colMuted = new Color32(90, 98, 108, 255);

    private readonly Color colDanger = new Color32(204, 46, 46, 255);
    private readonly Color colGood = new Color32(30, 148, 77, 255);
    private readonly Color colWarn = new Color32(227, 149, 0, 255);

    private readonly Color colPrimary = new Color32(0, 140, 70, 255);
    private readonly Color colAccent = new Color32(0, 120, 205, 255);
    private readonly Color colLink = new Color32(0, 120, 205, 255);
    private readonly Color colLinkHover = new Color32(0, 160, 255, 255);

    private enum PasswordGrade { Unknown, Weak, Medium, Strong }

    void OnEnable()
    {
        EnsureEventSystem();
        EnsureUICamera();

        if (autoRebuild) BuildIfMissing();

        ApplyPlacement();
        CacheAndWire();
        RefreshHintState();
        ApplyHackedUI();
        RefreshRewardHUD();
    }

    void OnValidate()
    {
        if (!enabled) return;

        if (autoRebuild) BuildIfMissing();

        ApplyPlacement();
        CacheAndWire();
        ApplyHackedUI();
        RefreshRewardHUD();
    }

    void Update()
    {
        ApplyPlacement();

        if (Application.isPlaying)
        {
            PulseHintGlows();
            RefreshHintState();
            AnimateSuspiciousDomainFlash();
            AnimateHackedBigAlert();
            AnimateRewardToast();
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
        ApplyHackedUI();
        RefreshRewardHUD();
    }

    void BuildIfMissing()
    {
        var existing = transform.Find(rootName);
        if (!existing) BuildAll();
        else CacheRefs(existing);
    }

    void CacheRefs(Transform existingRoot)
    {
        root = existingRoot.GetComponent<RectTransform>();
        canvas = existingRoot.GetComponent<Canvas>();
        if (canvas) canvas.worldCamera = uiCamera;

        desktop = existingRoot.Find("Desktop")?.GetComponent<RectTransform>();
        if (!desktop) return;

        homeWindow = desktop.Find("HomeWindow")?.GetComponent<RectTransform>();
        dashboardWindow = desktop.Find("DashboardWindow")?.GetComponent<RectTransform>();
        mailWindow = desktop.Find("MailWindow")?.GetComponent<RectTransform>();
        browserWindow = desktop.Find("BrowserWindow")?.GetComponent<RectTransform>();
        resultWindow = desktop.Find("ResultWindow")?.GetComponent<RectTransform>();

        // HUD
        hudRoot = desktop.Find("RewardHUD")?.GetComponent<RectTransform>();
        if (hudRoot)
        {
            hudScore = hudRoot.Find("Score")?.GetComponent<TextMeshProUGUI>();
            hudBadge = hudRoot.Find("Badge")?.GetComponent<TextMeshProUGUI>();
            hudToast = hudRoot.Find("Toast/ToastText")?.GetComponent<TextMeshProUGUI>();
            var toastBgRT = hudRoot.Find("Toast")?.GetComponent<RectTransform>();
            hudToastBg = toastBgRT ? toastBgRT.GetComponent<Image>() : null;
        }

        hackedOverlay = desktop.Find("HackedOverlay")?.GetComponent<RectTransform>();
        hackedOverlayImage = hackedOverlay ? hackedOverlay.GetComponent<Image>() : null;

        hackedBigPanel = desktop.Find("HackedBIG")?.GetComponent<RectTransform>();
        if (hackedBigPanel)
        {
            hackedBigTitle = hackedBigPanel.Find("Title")?.GetComponent<TextMeshProUGUI>();
            hackedBigBody = hackedBigPanel.Find("Body")?.GetComponent<TextMeshProUGUI>();
            hackedSirenLeft = hackedBigPanel.Find("SirenLeft")?.GetComponent<RectTransform>();
            hackedSirenRight = hackedBigPanel.Find("SirenRight")?.GetComponent<RectTransform>();
        }

        // HOME
        if (homeWindow)
        {
            var body = homeWindow.Find("Body")?.GetComponent<RectTransform>();
            homeWelcomeLine = body?.Find("Hero/Welcome")?.GetComponent<TextMeshProUGUI>();
            homeSearchField = body?.Find("Hero/SearchField")?.GetComponent<TMP_InputField>();
            btnHomeSignIn = FindButton(homeWindow, "SignInBtn");
            btnHomeRegister = FindButton(homeWindow, "RegisterBtn");
        }

        // DASH
        if (dashboardWindow)
        {
            var body = dashboardWindow.Find("Body")?.GetComponent<RectTransform>();
            dashTitle = body?.Find("Top/Title")?.GetComponent<TextMeshProUGUI>();
            dashSub = body?.Find("Top/Sub")?.GetComponent<TextMeshProUGUI>();
            dashHint = body?.Find("HintCard/HintText")?.GetComponent<TextMeshProUGUI>();

            dashAlertBanner = body?.Find("AlertBanner")?.GetComponent<RectTransform>();
            dashAlertText = dashAlertBanner?.Find("Text")?.GetComponent<TextMeshProUGUI>();

            btnDashOpenInbox = FindButton(dashboardWindow, "InboxBtn");
            btnDashLogout = FindButton(dashboardWindow, "LogoutBtn");
            btnDashBackHome = FindButton(dashboardWindow, "BackHomeBtn");
        }

        // MAIL
        if (mailWindow)
        {
            var body = mailWindow.Find("Body")?.GetComponent<RectTransform>();
            mailTitleLine = body?.Find("RightPane/MailHeader/Title")?.GetComponent<TextMeshProUGUI>();
            mailFromLine = body?.Find("RightPane/MailHeader/From")?.GetComponent<TextMeshProUGUI>();
            mailSubjectLine = body?.Find("RightPane/MailHeader/Subject")?.GetComponent<TextMeshProUGUI>();
            mailBodyText = body?.Find("RightPane/MailBody")?.GetComponent<TextMeshProUGUI>();
            mailHintBox = body?.Find("RightPane/HintBox/HintText")?.GetComponent<TextMeshProUGUI>();

            btnMailBack = FindButton(mailWindow, "BackBtn");
            btnMailReport = FindButton(mailWindow, "ReportBtn");
            btnMailDelete = FindButton(mailWindow, "DeleteBtn");
            btnMailVerify = FindButton(mailWindow, "VerifyBtn");
            btnMailOpenItem = FindButton(mailWindow, "OpenMailItemBtn");
        }

        // BROWSER
        if (browserWindow)
        {
            var body = browserWindow.Find("Body")?.GetComponent<RectTransform>();

            browserStatus = body?.Find("TopBar/Status")?.GetComponent<TextMeshProUGUI>();
            browserSecureLabel = body?.Find("TopBar/SecureLabel")?.GetComponent<TextMeshProUGUI>();
            browserSecureDot = body?.Find("TopBar/SecureDot")?.GetComponent<Image>();
            browserUrlText = body?.Find("TopBar/UrlText")?.GetComponent<TextMeshProUGUI>();

            browserPageRoot = body?.Find("Page")?.GetComponent<RectTransform>();
            browserSuccessRoot = body?.Find("Success")?.GetComponent<RectTransform>();

            browserUserField = browserPageRoot?.Find("UserField")?.GetComponent<TMP_InputField>();
            browserPassField = browserPageRoot?.Find("PasswordField")?.GetComponent<TMP_InputField>();
            browserError = browserPageRoot?.Find("Error")?.GetComponent<TextMeshProUGUI>();
            browserPageTitle = browserPageRoot?.Find("PageTitle")?.GetComponent<TextMeshProUGUI>();
            browserPageSub = browserPageRoot?.Find("PageSub")?.GetComponent<TextMeshProUGUI>();
            browserHintBox = browserPageRoot?.Find("HintBox/HintText")?.GetComponent<TextMeshProUGUI>();

            btnBrowserBack = FindButton(browserWindow, "BackBtn");
            btnBrowserSubmit = FindButton(browserWindow, "SubmitBtn");
        }

        // RESULT
        if (resultWindow)
        {
            btnResultRetry = FindButton(resultWindow, "RetryBtn");
            btnResultClose = FindButton(resultWindow, "CloseBtn");
        }
    }

    // =========================
    // Placement
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
        BuildRewardHUD();
        BuildHackedOverlay();
        BuildHackedBIG();

        BuildHomeWindow();
        BuildDashboardWindow();
        BuildMailWindow();
        BuildBrowserWindow();
        BuildResultWindow();

        SwitchState(AppState.Home);
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
    }

    void BuildRewardHUD()
    {
        hudRoot = UI.Panel(desktop, "RewardHUD", new Color32(0, 0, 0, 0),
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);

        // Container top-right
        var box = UI.Panel(hudRoot, "HUDBox", new Color32(255, 255, 255, 230),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-320, -14), new Vector2(-14, -104));
        UI.AddSoftOutline(box.gameObject, new Color32(215, 222, 232, 255), 2);

        hudScore = UI.Text(box, "Score", "Score: 0", 18, colText,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(12, 44), new Vector2(-12, -12),
            TextAlignmentOptions.TopLeft);

        hudBadge = UI.Text(box, "Badge", "Badge: —", 16, colMuted,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(12, 14), new Vector2(-12, -36),
            TextAlignmentOptions.TopLeft);

        // Toast (under HUDBox)
        var toast = UI.Panel(hudRoot, "Toast", new Color32(0, 0, 0, 0),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-520, -110), new Vector2(-14, -170));
        hudToastBg = toast.GetComponent<Image>();
        hudToastBg.color = new Color32(0, 120, 205, 0);
        UI.AddSoftOutline(toast.gameObject, new Color32(0, 0, 0, 0), 0);

        hudToast = UI.Text(toast, "ToastText", "", 16, Color.white,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(14, 8), new Vector2(-14, -8),
            TextAlignmentOptions.MidlineLeft);
        hudToast.enableWordWrapping = true;

        toast.gameObject.SetActive(false);
    }

    void BuildHackedOverlay()
    {
        hackedOverlay = UI.Panel(desktop, "HackedOverlay", new Color32(0, 0, 0, 0),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        hackedOverlayImage = hackedOverlay.GetComponent<Image>();
        hackedOverlayImage.raycastTarget = false;
        hackedOverlay.gameObject.SetActive(false);
    }

    void BuildHackedBIG()
    {
        // Huge alert card in the center of the monitor
        hackedBigPanel = UI.Panel(desktop, "HackedBIG", new Color32(20, 20, 22, 245),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-520, -220), new Vector2(520, 220));
        UI.AddSoftOutline(hackedBigPanel.gameObject, new Color32(255, 90, 90, 255), 4);

        // Siren bars
        hackedSirenLeft = UI.Panel(hackedBigPanel, "SirenLeft", new Color32(220, 30, 30, 220),
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(30, 0));
        hackedSirenRight = UI.Panel(hackedBigPanel, "SirenRight", new Color32(220, 30, 30, 220),
            new Vector2(1, 0), new Vector2(1, 1), new Vector2(-30, 0), new Vector2(0, 0));

        hackedBigTitle = UI.Text(hackedBigPanel, "Title", "⚠️  ACCOUNT COMPROMISED  ⚠️", 44, new Color32(255, 80, 80, 255),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -20), new Vector2(-24, -96),
            TextAlignmentOptions.TopLeft);
        hackedBigTitle.enableWordWrapping = true;

        hackedBigBody = UI.Text(hackedBigPanel, "Body",
            "YOU ENTERED YOUR PASSWORD ON A FAKE PAGE.\n\n" +
            "ACTION REQUIRED:\n" +
            "• Change password NOW\n" +
            "• Enable 2FA/MFA\n" +
            "• Report the email\n" +
            "• Type the official URL yourself",
            22, new Color32(255, 240, 240, 255),
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(24, 24), new Vector2(-24, -106),
            TextAlignmentOptions.TopLeft);
        hackedBigBody.enableWordWrapping = true;

        hackedBigPanel.gameObject.SetActive(false);
    }

    // -------------------- WINDOWS BUILD (your original) --------------------
    void BuildHomeWindow()
    {
        homeWindow = UI.Window(desktop, "HomeWindow", "eCitizen",
            homeWindowPos, homeWindowSize, colWindow, colBorder, colText,
            onClose: () => { });

        var body = homeWindow.Find("Body").GetComponent<RectTransform>();

        var header = UI.Panel(body, "Header", Color.white,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -78), new Vector2(0, 0));
        UI.Image(header, "BottomBorder", colBorder, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1));

        var crest = UI.Panel(header, "Crest", new Color32(0, 0, 0, 0),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, -22), new Vector2(58, 22));
        UI.Text(crest, "CrestTxt", "KE", 18, colPrimary, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);

        UI.Text(header, "Brand", "eCitizen", 28, colText,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(68, 10), new Vector2(240, -10),
            TextAlignmentOptions.MidlineLeft);

        UI.Text(header, "Nav", "Home   •   National   •   Counties   •   Help & Support", 16, colMuted,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(240, 10), new Vector2(-320, -10),
            TextAlignmentOptions.MidlineLeft);

        UI.Button(header, "SignInBtn", "Sign in", 18, new Color32(235, 237, 241, 255), colText,
            new Vector2(1, 0), new Vector2(1, 1), new Vector2(-300, 14), new Vector2(-190, -14),
            () => { });

        UI.Button(header, "RegisterBtn", "Register", 18, colPrimary, Color.white,
            new Vector2(1, 0), new Vector2(1, 1), new Vector2(-180, 14), new Vector2(-40, -14),
            () => { });

        var hero = UI.Panel(body, "Hero", new Color32(230, 247, 238, 255),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -92), new Vector2(-18, -300));
        UI.AddSoftOutline(hero.gameObject, colBorder, 2);

        homeWelcomeLine = UI.Text(hero, "Welcome", "Government of Kenya services simplified.\nAll your records unified.", 26, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -18), new Vector2(-20, -92),
            TextAlignmentOptions.TopLeft);
        homeWelcomeLine.enableWordWrapping = true;

        homeSearchField = UI.Input(hero, "SearchField", "Search for a service e.g. passport, good conduct, KRA…", 18,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -106), new Vector2(-20, -160),
            isPassword: false);

        UI.Text(hero, "Hint", "Lesson: Password protection + phishing. Always confirm the official domain before signing in.", 16, colMuted,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -164), new Vector2(-20, -210),
            TextAlignmentOptions.TopLeft).enableWordWrapping = true;

        var banner = UI.Panel(body, "Banner", new Color32(0, 120, 110, 255),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(18, 18), new Vector2(-18, 170));
        UI.AddSoftOutline(banner.gameObject, colBorder, 2);

        UI.Text(banner, "BannerTitle", "Get started on eCitizen today", 26, Color.white,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -20), new Vector2(-20, -70),
            TextAlignmentOptions.TopLeft);

        UI.Text(banner, "BannerSub", "Sign in to view your services. Then check your inbox for a security message.", 18, new Color32(235, 245, 245, 255),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -72), new Vector2(-20, -120),
            TextAlignmentOptions.TopLeft).enableWordWrapping = true;
    }

    void BuildDashboardWindow()
    {
        dashboardWindow = UI.Window(desktop, "DashboardWindow", "eCitizen Dashboard",
            dashboardWindowPos, dashboardWindowSize, colWindow, colBorder, colText,
            onClose: () => SwitchState(AppState.Home));

        var body = dashboardWindow.Find("Body").GetComponent<RectTransform>();

        var top = UI.Panel(body, "Top", new Color32(0, 0, 0, 0),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -18), new Vector2(-18, -140));

        dashTitle = UI.Text(top, "Title", "Welcome.", 32, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -56),
            TextAlignmentOptions.TopLeft);

        dashSub = UI.Text(top, "Sub", "Check your inbox for a security message.", 18, colMuted,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -54), new Vector2(0, -94),
            TextAlignmentOptions.TopLeft);

        UI.Button(top, "BackHomeBtn", "← Home", 18, new Color32(235, 237, 241, 255), colText,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-360, 10), new Vector2(-250, 50),
            () => { });

        UI.Button(top, "LogoutBtn", "Logout", 18, new Color32(235, 237, 241, 255), colText,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-240, 10), new Vector2(-120, 50),
            () => { });

        dashAlertBanner = UI.Panel(body, "AlertBanner", new Color32(255, 235, 235, 255),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -150), new Vector2(-18, -210));
        UI.AddSoftOutline(dashAlertBanner.gameObject, new Color32(245, 170, 170, 255), 2);

        dashAlertText = UI.Text(dashAlertBanner, "Text", "SECURITY ALERT: Suspicious login detected. Change your password immediately.", 18, colDanger,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 10), new Vector2(-16, -10),
            TextAlignmentOptions.MidlineLeft);
        dashAlertText.enableWordWrapping = true;

        var inboxCard = UI.Panel(body, "InboxCard", Color.white,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -230), new Vector2(-18, -420));
        UI.AddSoftOutline(inboxCard.gameObject, colBorder, 2);

        UI.Text(inboxCard, "InboxTitle", "Inbox", 22, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -14), new Vector2(-16, -50),
            TextAlignmentOptions.TopLeft);

        UI.Text(inboxCard, "InboxSub", "1 new message • Security notice", 16, colMuted,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -52), new Vector2(-16, -86),
            TextAlignmentOptions.TopLeft);

        UI.Button(inboxCard, "InboxBtn", "Open Inbox →", 18, colPrimary, Color.white,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-220, 18), new Vector2(-16, 56),
            () => { });

        var hintCard = UI.Panel(body, "HintCard", new Color32(235, 242, 255, 255),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -440), new Vector2(-18, -640));
        UI.AddSoftOutline(hintCard.gameObject, new Color32(200, 220, 255, 255), 2);

        dashHint = UI.Text(hintCard, "HintText",
            "Lesson goals:\n" +
            "• Password protection (use strong passwords)\n" +
            "• Phishing (spot urgency + fake domain)\n\n" +
            "Next: Open your inbox and inspect the message.",
            18, colText,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 14), new Vector2(-16, -14),
            TextAlignmentOptions.TopLeft);
        dashHint.enableWordWrapping = true;
    }

    void BuildMailWindow()
    {
        mailWindow = UI.Window(desktop, "MailWindow", "Inbox",
            mailWindowPos, mailWindowSize, colWindow, colBorder, colText,
            onClose: () => SwitchState(AppState.Dashboard));

        var body = mailWindow.Find("Body").GetComponent<RectTransform>();

        var ribbon = UI.Panel(body, "Ribbon", new Color32(252, 252, 253, 255),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -74), new Vector2(0, 0));
        UI.Image(ribbon, "BottomBorder", colBorder, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 1));

        UI.Button(ribbon, "BackBtn", "← Dashboard", 18, new Color32(235, 237, 241, 255), colText,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(12, 12), new Vector2(170, -12),
            () => { });

        UI.Button(ribbon, "ReportBtn", "Report phishing", 18, new Color32(235, 237, 241, 255), colText,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(190, 12), new Vector2(360, -12),
            () => { });

        UI.Button(ribbon, "DeleteBtn", "Delete", 18, new Color32(235, 237, 241, 255), colText,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(378, 12), new Vector2(480, -12),
            () => { });

        var left = UI.Panel(body, "LeftPane", new Color32(247, 248, 250, 255),
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(320, -74));
        UI.Image(left, "RightBorder", colBorder, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-1, 0), Vector2.zero);

        UI.Text(left, "Folders", "Inbox", 22, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -16), new Vector2(-16, -56),
            TextAlignmentOptions.TopLeft);

        var mailItem = UI.Button(left, "OpenMailItemBtn", "", 18, new Color32(235, 242, 255, 255), colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -64), new Vector2(-12, -160),
            () => { });
        var itemRT = mailItem.GetComponent<RectTransform>();

        UI.Text(itemRT, "From", "From: eCitizen Support <support@ecitizen.go-ke.com>", 14, colMuted,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -10), new Vector2(-12, -34),
            TextAlignmentOptions.TopLeft).enableWordWrapping = true;

        UI.Text(itemRT, "Subject", "URGENT: Verify your account to avoid suspension", 18, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -32), new Vector2(-12, -74),
            TextAlignmentOptions.TopLeft).enableWordWrapping = true;

        UI.Text(itemRT, "Snippet", "We detected unusual activity. Verify within 30 minutes…", 14, colMuted,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -72), new Vector2(-12, -112),
            TextAlignmentOptions.TopLeft).enableWordWrapping = true;

        var right = UI.Panel(body, "RightPane", Color.white,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(320, 0), new Vector2(0, -74));
        UI.AddSoftOutline(right.gameObject, colBorder, 2);

        var header = UI.Panel(right, "MailHeader", new Color32(0, 0, 0, 0),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -16), new Vector2(-16, -140));

        mailTitleLine = UI.Text(header, "Title", "Security message", 24, colText,
            new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, -40),
            TextAlignmentOptions.TopLeft);

        mailFromLine = UI.Text(header, "From", "From: eCitizen Support <support@ecitizen.go-ke.com>", 16, colMuted,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -42), new Vector2(0, -72),
            TextAlignmentOptions.TopLeft);
        mailFromLine.enableWordWrapping = true;

        mailSubjectLine = UI.Text(header, "Subject", "Subject: URGENT: Verify your account to avoid suspension", 16, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -74), new Vector2(0, -110),
            TextAlignmentOptions.TopLeft);
        mailSubjectLine.enableWordWrapping = true;

        var verifyGO = UI.Button(header, "VerifyBtn", $"VERIFY NOW  →  {suspiciousDomain}", 16,
            new Color32(235, 242, 255, 255), colLink,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-430, 0), new Vector2(-16, 44),
            () => { });

        var verifyLabel = verifyGO.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (verifyLabel)
        {
            verifyLabel.fontStyle = FontStyles.Underline;
            verifyLabel.richText = true;
            UI.AddHoverTextColor(verifyGO, verifyLabel, colLink, colLinkHover);
        }
        UI.AddHoverScale(verifyGO, 1.00f, 1.03f);

        mailBodyText = UI.Text(right, "MailBody", BuildMailBody(), 18, colText,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 120), new Vector2(-16, -160),
            TextAlignmentOptions.TopLeft);
        mailBodyText.enableWordWrapping = true;

        var hintBox = UI.Panel(right, "HintBox", new Color32(255, 250, 235, 255),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 16), new Vector2(-16, 108));
        UI.AddSoftOutline(hintBox.gameObject, new Color32(245, 220, 160, 255), 2);

        mailHintBox = UI.Text(hintBox, "HintText",
            "Hints:\n• Urgency (“30 minutes”) is a red flag\n• Sender domain looks wrong (go-ke)\n• Don’t type passwords from email links\n\nGood actions: Report or Delete.",
            16, colText,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(12, 10), new Vector2(-12, -10),
            TextAlignmentOptions.TopLeft);
        mailHintBox.enableWordWrapping = true;
    }

    void BuildBrowserWindow()
    {
        browserWindow = UI.Window(desktop, "BrowserWindow", "Browser",
            browserWindowPos, browserWindowSize, colWindow, colBorder, colText,
            onClose: () => SwitchState(AppState.Mail));

        var body = browserWindow.Find("Body").GetComponent<RectTransform>();

        var top = UI.Panel(body, "TopBar", new Color32(252, 252, 253, 255),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72), new Vector2(0, 0));
        UI.Image(top, "BottomBorder", colBorder, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 1));

        UI.Button(top, "BackBtn", "←", 18, new Color32(235, 237, 241, 255), colText,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(12, 12), new Vector2(60, -12),
            () => { });

        browserSecureDot = UI.Image(top, "SecureDot", colGood,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(72, -8), new Vector2(88, 8));

        browserSecureLabel = UI.Text(top, "SecureLabel", "Secure", 16, colGood,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(92, 12), new Vector2(170, -12),
            TextAlignmentOptions.MidlineLeft);

        browserUrlText = UI.Text(top, "UrlText", $"https://{officialDomain}", 16, colText,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(180, 12), new Vector2(-240, -12),
            TextAlignmentOptions.MidlineLeft);

        browserStatus = UI.Text(top, "Status", "", 16, colMuted,
            new Vector2(1, 0), new Vector2(1, 1), new Vector2(-240, 12), new Vector2(-16, -12),
            TextAlignmentOptions.MidlineRight);

        browserPageRoot = UI.Panel(body, "Page", Color.white,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(18, 18), new Vector2(-18, -90));
        UI.AddSoftOutline(browserPageRoot.gameObject, new Color32(226, 230, 236, 255), 2);

        browserPageTitle = UI.Text(browserPageRoot, "PageTitle", "Sign in", 32, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -26), new Vector2(-26, -86),
            TextAlignmentOptions.TopLeft);

        browserPageSub = UI.Text(browserPageRoot, "PageSub", "", 18, colMuted,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -86), new Vector2(-26, -140),
            TextAlignmentOptions.TopLeft);
        browserPageSub.enableWordWrapping = true;

        UI.Text(browserPageRoot, "UserLabel", "Email / Phone", 18, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -170), new Vector2(-26, -204),
            TextAlignmentOptions.TopLeft);

        browserUserField = UI.Input(browserPageRoot, "UserField", "e.g. name@email.com or 07xx...", 18,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -210), new Vector2(-26, -270),
            false);

        UI.Text(browserPageRoot, "PassLabel", "Password", 18, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -290), new Vector2(-26, -324),
            TextAlignmentOptions.TopLeft);

        browserPassField = UI.Input(browserPageRoot, "PasswordField", "Enter your password…", 18,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -330), new Vector2(-26, -390),
            true);

        browserError = UI.Text(browserPageRoot, "Error", "", 16, colDanger,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -396), new Vector2(-26, -432),
            TextAlignmentOptions.TopLeft);
        browserError.gameObject.SetActive(false);

        var hintBox = UI.Panel(browserPageRoot, "HintBox", new Color32(235, 242, 255, 255),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(26, 18), new Vector2(-26, 110));
        UI.AddSoftOutline(hintBox.gameObject, new Color32(200, 220, 255, 255), 2);

        browserHintBox = UI.Text(hintBox, "HintText", "", 16, colText,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(12, 10), new Vector2(-12, -10),
            TextAlignmentOptions.TopLeft);
        browserHintBox.enableWordWrapping = true;

        var submitGO = UI.Button(browserPageRoot, "SubmitBtn", "Continue", 20, colPrimary, Color.white,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-240, -470), new Vector2(-26, -424),
            () => { });
        UI.AddHoverScale(submitGO, 1.00f, 1.03f);

        browserSuccessRoot = UI.Panel(body, "Success", Color.white,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(18, 18), new Vector2(-18, -90));
        UI.AddSoftOutline(browserSuccessRoot.gameObject, new Color32(226, 230, 236, 255), 2);

        UI.Text(browserSuccessRoot, "SuccessTitle", "Processing…", 34, colGood,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -36), new Vector2(-26, -110),
            TextAlignmentOptions.TopLeft);

        UI.Text(browserSuccessRoot, "SuccessBody",
            "If this was a phishing page, you may be compromised even if it says “success”.",
            18, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -110), new Vector2(-26, -190),
            TextAlignmentOptions.TopLeft).enableWordWrapping = true;

        browserSuccessRoot.gameObject.SetActive(false);
    }

    void BuildResultWindow()
    {
        resultWindow = UI.Window(desktop, "ResultWindow", "Lesson Outcome",
            resultWindowPos, resultWindowSize, colWindow, colBorder, colText,
            onClose: () => ShowWindow(resultWindow, false));

        var body = resultWindow.Find("Body").GetComponent<RectTransform>();

        UI.Text(body, "OutcomeTitle", "—", 34, colText,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -26), new Vector2(-26, -88),
            TextAlignmentOptions.TopLeft);

        UI.Text(body, "OutcomeBody", "—", 18, colText,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(26, 120), new Vector2(-26, -92),
            TextAlignmentOptions.TopLeft);

        UI.Button(body, "RetryBtn", "Retry", 20, colPrimary, Color.white,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-240, 26), new Vector2(-26, 86),
            () => { });

        UI.Button(body, "CloseBtn", "Close", 20, new Color32(235, 237, 241, 255), colText,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-380, 26), new Vector2(-260, 86),
            () => { });
    }

    // =========================
    // State / UI Updates
    // =========================
    void SwitchState(AppState newState)
    {
        state = newState;

        ShowWindow(homeWindow, state == AppState.Home);
        ShowWindow(browserWindow, state == AppState.BrowserOfficialLogin || state == AppState.BrowserPhishVerify);
        ShowWindow(dashboardWindow, state == AppState.Dashboard);
        ShowWindow(mailWindow, state == AppState.Mail);
        ShowWindow(resultWindow, state == AppState.Result);

        if (state == AppState.Home) UpdateHome();
        if (state == AppState.Dashboard) UpdateDashboard();
        if (state == AppState.Mail) UpdateMail();
        if (state == AppState.BrowserOfficialLogin) PrepareBrowser(isPhish: false);
        if (state == AppState.BrowserPhishVerify) PrepareBrowser(isPhish: true);

        RefreshHintState();
        ApplyHackedUI();
        RefreshRewardHUD();
    }

    void UpdateHome()
    {
        if (homeWelcomeLine)
            homeWelcomeLine.text = "Government of Kenya services simplified.\nAll your government records unified.";
    }

    void UpdateDashboard()
    {
        if (dashTitle) dashTitle.text = loggedIn ? $"Welcome, {citizenIdOrEmail}." : "Welcome.";
        if (dashSub) dashSub.text = "New: You have 1 security message in your inbox.";

        if (dashAlertBanner) dashAlertBanner.gameObject.SetActive(hacked);
        if (dashAlertText)
        {
            dashAlertText.text = hacked
                ? "SECURITY ALERT: Your account may be compromised. Change password + enable 2FA immediately."
                : "";
        }

        if (dashHint)
        {
            dashHint.text =
                "What to do:\n" +
                "1) Open Inbox\n" +
                "2) Inspect sender + domain + urgency\n" +
                "3) Rewarded actions: Report or Delete\n" +
                "4) Risky: Clicking a link and entering your password";
        }
    }

    void UpdateMail()
    {
        if (!openedInbox)
        {
            openedInbox = true;
            AddScore(10, "Opened inbox (good awareness)");
            UpdateDashboard();
        }

        if (mailTitleLine) mailTitleLine.text = "Security message";
        if (mailFromLine) mailFromLine.text = "From: eCitizen Support <support@ecitizen.go-ke.com>";
        if (mailSubjectLine) mailSubjectLine.text = "Subject: URGENT: Verify your account to avoid suspension";
        if (mailBodyText) mailBodyText.text = BuildMailBody();

        if (mailHintBox)
        {
            mailHintBox.text =
                "Hints (What’s wrong here):\n" +
                "• Urgency (“30 minutes”) pushes panic\n" +
                "• Sender domain is NOT official\n" +
                $"• Official should look like: {officialDomain}\n" +
                "• Legit orgs rarely ask for your password from a link\n\n" +
                "Good actions: Report or Delete (you earn points).";
        }
    }

    // =========================
    // Flow Actions
    // =========================
    void OpenOfficialLogin()
    {
        clickedSuspiciousVerify = false;
        submittedOnPhishPage = false;
        SwitchState(AppState.BrowserOfficialLogin);

        if (Application.isPlaying) StartCoroutine(BrowserLoadRoutine());
        else
        {
            if (browserStatus) browserStatus.text = "Secure";
            if (browserPageRoot) browserPageRoot.gameObject.SetActive(true);
        }
    }

    void OpenInbox()
    {
        SwitchState(AppState.Mail);
    }

    void ReportEmail()
    {
        if (reportedEmail) return;
        reportedEmail = true;

        AddScore(30, "Reported phishing (best action)");
        ShowOutcome(
            "GOOD DECISION ✅",
            colGood,
            "You reported the suspicious email.\n\n" +
            "Why this is correct:\n" +
            "• Reporting protects other users\n" +
            "• You did not engage with the link\n\n" +
            "Next time:\n" +
            "• Verify by typing the official site directly"
        );
        SwitchState(AppState.Result);
    }

    void DeleteEmail()
    {
        if (deletedEmail) return;
        deletedEmail = true;

        AddScore(20, "Deleted suspicious email (good action)");
        ShowOutcome(
            "GOOD DECISION ✅",
            colGood,
            "You deleted the suspicious email.\n\n" +
            "Why this is correct:\n" +
            "• Reduces the chance you click it later\n" +
            "• Avoids credential theft\n\n" +
            "Even better:\n" +
            "• Report it as phishing too"
        );
        SwitchState(AppState.Result);
    }

    void ClickVerifyLink()
    {
        clickedSuspiciousVerify = true;
        AddScore(-5, "Clicked suspicious link (bad move)");
        SwitchState(AppState.BrowserPhishVerify);

        if (Application.isPlaying) StartCoroutine(BrowserLoadRoutine());
        else
        {
            if (browserStatus) browserStatus.text = "Not secure";
            if (browserPageRoot) browserPageRoot.gameObject.SetActive(true);
        }
    }

    IEnumerator BrowserLoadRoutine()
    {
        yield return new WaitForSeconds(browserLoadSeconds);
        if (browserStatus) browserStatus.text = (state == AppState.BrowserPhishVerify) ? "Not secure" : "Secure";
        if (browserPageRoot) browserPageRoot.gameObject.SetActive(true);
        RefreshHintState();
    }

    void PrepareBrowser(bool isPhish)
    {
        if (browserPageRoot) browserPageRoot.gameObject.SetActive(false);
        if (browserSuccessRoot) browserSuccessRoot.gameObject.SetActive(false);
        ClearBrowserError();

        if (browserStatus) browserStatus.text = "Loading…";

        if (!isPhish)
        {
            if (browserUrlText) browserUrlText.text = $"https://{officialDomain}";
            if (browserSecureDot) browserSecureDot.color = colGood;
            if (browserSecureLabel) { browserSecureLabel.text = "Secure"; browserSecureLabel.color = colGood; }

            if (browserPageTitle) browserPageTitle.text = "Sign in to eCitizen";
            if (browserPageSub) browserPageSub.text = "Official sign-in. Good practice: use a strong password.";
            if (browserHintBox)
                browserHintBox.text =
                    "Password tips:\n" +
                    "• 12+ characters\n" +
                    "• Mix UPPER/lower, numbers, symbols\n" +
                    "• Avoid common words (kenya, nairobi, password)\n" +
                    "• Use MFA/2FA";
        }
        else
        {
            if (browserUrlText) browserUrlText.text = $"https://{suspiciousDomain}";
            if (browserSecureDot) browserSecureDot.color = colDanger;
            if (browserSecureLabel) { browserSecureLabel.text = "Not secure"; browserSecureLabel.color = colDanger; }

            if (browserPageTitle) browserPageTitle.text = "Urgent Verification Required";
            if (browserPageSub) browserPageSub.text =
                "To avoid suspension, confirm your password now.\n(This is a classic phishing pattern.)";
            if (browserHintBox)
                browserHintBox.text =
                    "What’s wrong here:\n" +
                    "• Urgency + threat\n" +
                    "• Not official domain\n" +
                    "• Asking for password from a link\n\n" +
                    "Correct action: close page, report email.";
        }

        if (browserUserField) browserUserField.text = citizenIdOrEmail;
        if (browserPassField) browserPassField.text = "";
    }

    void OnBrowserSubmit()
    {
        if (!browserUserField || !browserPassField) return;

        var user = (browserUserField.text ?? "").Trim();
        var pass = (browserPassField.text ?? "");

        if (string.IsNullOrWhiteSpace(user))
        {
            SetBrowserError("Please enter email/phone.");
            return;
        }

        if (string.IsNullOrWhiteSpace(pass))
        {
            SetBrowserError("Password is required.");
            return;
        }

        citizenIdOrEmail = user;
        chosenPassword = pass;
        chosenGrade = GradePassword(chosenPassword);
        ClearBrowserError();

        if (browserStatus) browserStatus.text = "Processing…";
        if (Application.isPlaying) StartCoroutine(SubmitRoutine());
        else ShowPostSuccessThenOutcome();
    }

    IEnumerator SubmitRoutine()
    {
        yield return new WaitForSeconds(submitLoadSeconds);
        ShowPostSuccessThenOutcome();
    }

    void ShowPostSuccessThenOutcome()
    {
        if (browserPageRoot) browserPageRoot.gameObject.SetActive(false);
        if (browserSuccessRoot) browserSuccessRoot.gameObject.SetActive(true);
        if (browserStatus) browserStatus.text = "Complete";

        if (Application.isPlaying) StartCoroutine(PostSuccessDelayRoutine());
        else ResolveOutcome();
    }

    IEnumerator PostSuccessDelayRoutine()
    {
        yield return new WaitForSeconds(postSuccessDelay);
        ResolveOutcome();
    }

    void ResolveOutcome()
    {
        bool isPhish = (state == AppState.BrowserPhishVerify);

        if (!isPhish)
        {
            loggedIn = true;
            AddScore(15, "Signed in via official site");
            SwitchState(AppState.Dashboard);
            return;
        }

        submittedOnPhishPage = true;

        int roll = UnityEngine.Random.Range(0, 101);
        int chance = chosenGrade switch
        {
            PasswordGrade.Weak => phishHackChanceWeak,
            PasswordGrade.Medium => phishHackChanceMedium,
            PasswordGrade.Strong => phishHackChanceStrong,
            _ => 80
        };

        hacked = roll < chance;

        if (hacked)
        {
            AddScore(-40, "Entered password on phishing site");
            TriggerHackedAlert();

            ShowOutcome(
                "HACKED ❌",
                colDanger,
                $"You entered your password on a suspicious site.\n\n" +
                $"Attack succeeded (roll {roll} < {chance}%).\n" +
                $"Password strength: {chosenGrade}\n\n" +
                "This is a BIG DEAL.\nYour credentials may be stolen and used to access services."
            );
        }
        else
        {
            AddScore(-15, "Typed password on suspicious page (still wrong)");

            ShowOutcome(
                "PHISHING ATTEMPT (YOU GOT LUCKY) ⚠️",
                colWarn,
                $"You entered your password on a suspicious site.\n\n" +
                $"Attack failed (roll {roll} ≥ {chance}%).\n" +
                $"Password strength: {chosenGrade}\n\n" +
                "Lesson:\n" +
                "• Strong passwords reduce risk, but clicking & typing is still wrong.\n" +
                "Best action: report/delete and sign in via official site."
            );
        }

        SwitchState(AppState.Result);
    }

    void Logout()
    {
        loggedIn = false;
        openedInbox = false;
        clickedSuspiciousVerify = false;
        submittedOnPhishPage = false;
        reportedEmail = false;
        deletedEmail = false;
        hacked = false;

        chosenPassword = "";
        chosenGrade = PasswordGrade.Unknown;

        if (hackedFlashRoutine != null && Application.isPlaying) StopCoroutine(hackedFlashRoutine);
        hackedFlashRoutine = null;

        ApplyHackedUI();
        SwitchState(AppState.Home);
    }

    // =========================
    // Rewards / badge + HUD
    // =========================
    void AddScore(int delta, string reason)
    {
        score += delta;
        score = Mathf.Clamp(score, -200, 999);

        lastScoreDelta = delta;
        lastScoreReason = reason ?? "";
        lastScoreTime = Time.time;

        RefreshRewardHUD();
        ShowToast(delta, reason);
    }

    void RefreshRewardHUD()
    {
        if (!showRewardHUD) { if (hudRoot) hudRoot.gameObject.SetActive(false); return; }
        if (hudRoot) hudRoot.gameObject.SetActive(true);

        if (hudScore) hudScore.text = $"Score: {score}";
        if (hudBadge) hudBadge.text = $"Badge: {ComputeBadge(score)}";
    }

    void ShowToast(int delta, string reason)
    {
        if (!hudRoot || !hudToast || !hudToastBg) return;

        string sign = delta >= 0 ? "+" : "";
        string msg = $"{sign}{delta}  •  {reason}";
        hudToast.text = msg;

        // Color tone
        Color32 bg = (delta >= 0) ? new Color32(0, 140, 70, 220) : new Color32(204, 46, 46, 220);
        bg.a = 0;
        hudToastBg.color = bg;

        var toastRT = hudRoot.Find("Toast");
        if (toastRT) toastRT.gameObject.SetActive(true);
    }

    void AnimateRewardToast()
    {
        if (!hudRoot) return;
        var toastRT = hudRoot.Find("Toast");
        if (!toastRT) return;
        if (!toastRT.gameObject.activeSelf) return;

        float elapsed = Time.time - lastScoreTime;
        if (elapsed >= toastDuration)
        {
            toastRT.gameObject.SetActive(false);
            return;
        }

        // Fade in then out
        float half = toastDuration * 0.5f;
        float a = elapsed < half ? (elapsed / half) : (1f - (elapsed - half) / half);
        a = Mathf.Clamp01(a);

        if (hudToastBg)
        {
            var c = hudToastBg.color;
            c.a = (byte)Mathf.RoundToInt(220 * a);
            hudToastBg.color = c;
        }
        if (hudToast)
        {
            var tc = hudToast.color;
            tc.a = a;
            hudToast.color = tc;
        }

        // slight pop
        var tr = toastRT.GetComponent<RectTransform>();
        if (tr)
        {
            float s = 1f + 0.03f * Mathf.Sin(Time.time * 10f);
            tr.localScale = Vector3.one * s;
        }
    }

    string ComputeBadge(int s)
    {
        if (s >= 80) return "Cyber Smart 🛡️";
        if (s >= 40) return "Aware ✅";
        if (s >= 10) return "Learner 📘";
        if (s >= 0) return "Untrained ⚠️";
        return "At Risk ❌";
    }

    // =========================
    // Hacked alert visuals (BIG)
    // =========================
    void TriggerHackedAlert()
    {
        if (!Application.isPlaying) return;

        if (hackedFlashRoutine != null) StopCoroutine(hackedFlashRoutine);
        hackedFlashRoutine = StartCoroutine(HackedFlashRoutine());
    }

    IEnumerator HackedFlashRoutine()
    {
        if (!hackedOverlay) yield break;

        hackedOverlay.gameObject.SetActive(true);

        float t = 0f;
        while (hacked)
        {
            t += Time.deltaTime * hackedFlashSpeed;
            float a = 0.35f + 0.35f * Mathf.Sin(t);
            var c = hackedFlashColor;
            c.a = Mathf.Clamp01(a);
            if (hackedOverlayImage) hackedOverlayImage.color = c;
            yield return null;
        }

        hackedOverlay.gameObject.SetActive(false);
    }

    void ApplyHackedUI()
    {
        if (hackedOverlay)
        {
            hackedOverlay.gameObject.SetActive(hacked);
            if (!hacked && hackedOverlayImage) hackedOverlayImage.color = new Color(0, 0, 0, 0);
            if (hacked && hackedOverlayImage) hackedOverlayImage.color = hackedFlashColor;
        }

        if (hackedBigPanel)
            hackedBigPanel.gameObject.SetActive(hacked);

        if (hacked && hackedBigTitle)
            hackedBigTitle.text = "🚨  ACCOUNT COMPROMISED  🚨";

        if (hacked && hackedBigBody)
        {
            hackedBigBody.text =
                "YOU ENTERED YOUR PASSWORD ON A FAKE PAGE.\n\n" +
                "WHAT THIS MEANS:\n" +
                "• Your password can be stolen\n" +
                "• Attackers can log into your services\n" +
                "• They can reset accounts, steal data, or lock you out\n\n" +
                "DO THIS NOW:\n" +
                "1) Change password immediately\n" +
                "2) Enable 2FA/MFA\n" +
                "3) Report the phishing email\n" +
                $"4) Only sign in via: {officialDomain}";
        }
    }

    void AnimateHackedBigAlert()
    {
        if (!hacked || !hackedBigPanel) return;

        // Shake + pulse
        var rt = hackedBigPanel;
        float shakeX = Mathf.Sin(Time.time * hackedShakeSpeed) * hackedShakeAmount;
        float shakeY = Mathf.Cos(Time.time * hackedShakeSpeed * 1.2f) * hackedShakeAmount;
        rt.anchoredPosition = new Vector2(shakeX, shakeY);

        float p = 1f + 0.03f * (0.5f + 0.5f * Mathf.Sin(Time.time * hackedPulseSpeed * 6f));
        rt.localScale = Vector3.one * p;

        // Alternating siren bars
        if (hackedSirenLeft && hackedSirenRight)
        {
            float s = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
            var leftImg = hackedSirenLeft.GetComponent<Image>();
            var rightImg = hackedSirenRight.GetComponent<Image>();
            if (leftImg) leftImg.color = Color.Lerp(new Color32(220, 30, 30, 160), new Color32(255, 210, 60, 220), s);
            if (rightImg) rightImg.color = Color.Lerp(new Color32(255, 210, 60, 220), new Color32(220, 30, 30, 160), s);
        }

        // make overlay intensity stronger too
        if (hackedOverlayImage)
        {
            float a = 0.45f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
            var c = hackedFlashColor;
            c.a = Mathf.Clamp01(a);
            hackedOverlayImage.color = c;
        }
    }

    // =========================
    // Hint system
    // =========================
    void RefreshHintState()
    {
        if (!Application.isPlaying) return;

        if (state == AppState.Home) currentHint = HintTarget.HomeSignIn;
        else if (state == AppState.Dashboard) currentHint = HintTarget.DashInbox;
        else if (state == AppState.Mail) currentHint = HintTarget.MailSafeAction;
        else if (state == AppState.BrowserPhishVerify) currentHint = HintTarget.BrowserSubmit;
        else currentHint = HintTarget.None;
    }

    void PulseHintGlows()
    {
        float a = 0.25f + 0.75f * Mathf.PingPong(Time.time * hintPulseSpeed, 1f);

        SetHintAlpha(btnHomeSignIn, currentHint == HintTarget.HomeSignIn ? a : 0f);
        SetHintAlpha(btnDashOpenInbox, currentHint == HintTarget.DashInbox ? a : 0f);

        SetHintAlpha(btnMailReport, currentHint == HintTarget.MailSafeAction ? a : 0f);
        SetHintAlpha(btnMailDelete, currentHint == HintTarget.MailSafeAction ? a : 0f);
        SetHintAlpha(btnMailVerify, currentHint == HintTarget.MailSafeAction ? 0.10f : 0f);

        SetHintAlpha(btnBrowserSubmit, currentHint == HintTarget.BrowserSubmit ? a : 0f);
    }

    void AnimateSuspiciousDomainFlash()
    {
        if (!btnMailVerify) return;

        var label = btnMailVerify.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (!label) return;

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

    // =========================
    // Wiring
    // =========================
    void CacheAndWire()
    {
        btnHomeSignIn = FindButton(homeWindow, "SignInBtn");
        btnHomeRegister = FindButton(homeWindow, "RegisterBtn");

        btnDashOpenInbox = FindButton(dashboardWindow, "InboxBtn");
        btnDashLogout = FindButton(dashboardWindow, "LogoutBtn");
        btnDashBackHome = FindButton(dashboardWindow, "BackHomeBtn");

        btnMailBack = FindButton(mailWindow, "BackBtn");
        btnMailReport = FindButton(mailWindow, "ReportBtn");
        btnMailDelete = FindButton(mailWindow, "DeleteBtn");
        btnMailVerify = FindButton(mailWindow, "VerifyBtn");
        btnMailOpenItem = FindButton(mailWindow, "OpenMailItemBtn");

        btnBrowserBack = FindButton(browserWindow, "BackBtn");
        btnBrowserSubmit = FindButton(browserWindow, "SubmitBtn");

        btnResultRetry = FindButton(resultWindow, "RetryBtn");
        btnResultClose = FindButton(resultWindow, "CloseBtn");

        Wire(btnHomeSignIn, OpenOfficialLogin);
        Wire(btnHomeRegister, OpenOfficialLogin);

        Wire(btnDashOpenInbox, OpenInbox);
        Wire(btnDashBackHome, () => SwitchState(AppState.Home));
        Wire(btnDashLogout, Logout);

        Wire(btnMailBack, () => SwitchState(AppState.Dashboard));
        Wire(btnMailOpenItem, () => { });
        Wire(btnMailReport, ReportEmail);
        Wire(btnMailDelete, DeleteEmail);
        Wire(btnMailVerify, ClickVerifyLink);

        Wire(btnBrowserBack, () => SwitchState(AppState.Mail));
        Wire(btnBrowserSubmit, OnBrowserSubmit);

        Wire(btnResultRetry, () =>
        {
            clickedSuspiciousVerify = false;
            submittedOnPhishPage = false;
            reportedEmail = false;
            deletedEmail = false;

            hacked = false;
            ApplyHackedUI();

            if (browserPassField) browserPassField.text = "";
            ClearBrowserError();

            SwitchState(loggedIn ? AppState.Dashboard : AppState.Home);
        });

        Wire(btnResultClose, () => SwitchState(loggedIn ? AppState.Dashboard : AppState.Home));

        hintOutlines.Clear();
        RegisterHintOutline(btnHomeSignIn);
        RegisterHintOutline(btnDashOpenInbox);
        RegisterHintOutline(btnMailReport);
        RegisterHintOutline(btnMailDelete);
        RegisterHintOutline(btnMailVerify);
        RegisterHintOutline(btnBrowserSubmit);

        UpdateHome();
        UpdateDashboard();
        RefreshRewardHUD();
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
    // Result helpers (NOW includes "what you did wrong")
    // =========================
    void ShowOutcome(string title, Color titleColor, string body)
    {
        var bodyRT = resultWindow.Find("Body").GetComponent<RectTransform>();
        var t = bodyRT.Find("OutcomeTitle").GetComponent<TMP_Text>();
        var b = bodyRT.Find("OutcomeBody").GetComponent<TMP_Text>();

        t.text = title;
        t.color = titleColor;

        string mistakes = BuildMistakeSummary();
        string fixes = BuildFixSummary();

        b.text =
            body +
            "\n\n----------------------------\n" +
            "WHAT YOU DID WRONG:\n" + mistakes +
            "\n\nWHAT YOU SHOULD DO NEXT TIME:\n" + fixes +
            $"\n\nScore: {score} • Badge: {ComputeBadge(score)}";

        b.color = colText;
    }

    string BuildMistakeSummary()
    {
        var lines = new List<string>();

        if (clickedSuspiciousVerify)
            lines.Add("• You clicked a verification link from an email (high risk).");

        if (submittedOnPhishPage)
            lines.Add("• You typed your password on a suspicious page.");

        if (!reportedEmail && !deletedEmail && openedInbox)
            lines.Add("• You did not report or delete the suspicious email.");

        if (!openedInbox && loggedIn)
            lines.Add("• You ignored the inbox security message.");

        if (chosenGrade == PasswordGrade.Weak && submittedOnPhishPage)
            lines.Add("• Your password was weak, increasing your risk.");

        if (lines.Count == 0)
            lines.Add("• No major mistakes detected (good job).");

        return string.Join("\n", lines);
    }

    string BuildFixSummary()
    {
        var lines = new List<string>
        {
            $"• Always type the official site yourself: {officialDomain}",
            "• Never enter your password from an email link",
            "• Check the domain carefully (typosquats like \"go-ke\" are traps)",
            "• Report phishing emails (best) or delete them (good)",
            "• Use strong passwords + enable 2FA/MFA"
        };

        return string.Join("\n", lines);
    }

    void ShowWindow(RectTransform w, bool show)
    {
        if (w) w.gameObject.SetActive(show);
    }

    // =========================
    // Mail content
    // =========================
    string BuildMailBody()
    {
        return
            "Hello Citizen,\n\n" +
            "We detected unusual sign-in activity on your eCitizen account.\n" +
            "To avoid suspension, verify your account within 30 minutes.\n\n" +
            "Click the button above to verify.\n\n" +
            "Regards,\n" +
            "eCitizen Support Team\n\n" +
            "—\n" +
            "Training note: This message contains multiple phishing red flags.";
    }

    // =========================
    // Browser errors
    // =========================
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
    // Password grade
    // =========================
    PasswordGrade GradePassword(string p)
    {
        if (string.IsNullOrWhiteSpace(p)) return PasswordGrade.Unknown;

        int s = 0;
        if (p.Length >= 8) s += 20;
        if (p.Length >= 12) s += 25;
        if (p.Length >= 16) s += 10;

        if (Regex.IsMatch(p, "[a-z]")) s += 10;
        if (Regex.IsMatch(p, "[A-Z]")) s += 10;
        if (Regex.IsMatch(p, "[0-9]")) s += 10;
        if (Regex.IsMatch(p, @"[^a-zA-Z0-9]")) s += 15;

        if (Regex.IsMatch(p.ToLower(), @"password|qwerty|1234|admin|letmein|kenya|nairobi|ecitizen")) s -= 30;
        if (Regex.IsMatch(p, @"(.)\1\1")) s -= 10;
        if (Regex.IsMatch(p, @"12345|67890")) s -= 10;

        s = Mathf.Clamp(s, 0, 100);

        if (s < 45) return PasswordGrade.Weak;
        if (s < 75) return PasswordGrade.Medium;
        return PasswordGrade.Strong;
    }

    // =========================
    // Unity basics
    // =========================
    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

#if ENABLE_INPUT_SYSTEM
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
#else
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
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
            text.color = new Color32(24, 28, 33, 255);
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
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -54), Vector2.zero);

            Image(titleBar, "BottomBorder", border,
                new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 1));

            Text(titleBar, "Title", title, 20, textColor,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 6), new Vector2(-70, -6),
                TextAlignmentOptions.MidlineLeft);

            Button(titleBar, "Close", "✕", 18,
                new Color32(235, 237, 241, 255), textColor,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(-54, 8), new Vector2(-10, -8),
                onClose);

            Panel(frame, "Body", new Color32(0, 0, 0, 0),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, new Vector2(0, -54));

            return frame;
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

        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Flow: Home -> Sign in (official) -> Dashboard -> Inbox -> Report/Delete or click Verify.\n" +
            "Rewards: Report/Delete gives points. Clicking link and typing password triggers BIG red hacked alert.\n" +
            "End screen shows: WHAT YOU DID WRONG + WHAT TO DO NEXT.",
            MessageType.Info);
    }
}
#endif
