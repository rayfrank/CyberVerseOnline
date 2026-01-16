using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class MonitorUIScreenController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelHome;
    public GameObject panelLogs;
    public GameObject panelCameras;
    public GameObject panelConsole;

    [Header("Buttons")]
    public Button btnOpenLogs;
    public Button btnCameras;
    public Button btnAccessConsole;

    [Header("Logs UI")]
    public Text logsText;

    [Header("Console UI")]
    public Dropdown credentialDropdown; // options represent safe IDs
    public Button btnAttemptLogin;
    public Text consoleFeedback;

    [Header("Camera UI")]
    public Button btnDisableCameras;
    public Text cameraFeedback;

    [Header("Logic")]
    public Act1ConsoleLogic act1Logic;

    void Awake()
    {
        // Wire buttons
        if (btnOpenLogs) btnOpenLogs.onClick.AddListener(OpenLogs);
        if (btnCameras) btnCameras.onClick.AddListener(OpenCameras);
        if (btnAccessConsole) btnAccessConsole.onClick.AddListener(OpenConsole);

        if (btnAttemptLogin) btnAttemptLogin.onClick.AddListener(AttemptLogin);
        if (btnDisableCameras) btnDisableCameras.onClick.AddListener(DisableCameras);

        ShowOnly(panelHome);
    }

    void OnEnable()
    {
        if (CyberLabCampaignManager.I != null)
        {
            CyberLabCampaignManager.I.OnLogAdded += HandleLogAdded;
            // Build initial log view
            RefreshLogs();
        }
    }

    void OnDisable()
    {
        if (CyberLabCampaignManager.I != null)
            CyberLabCampaignManager.I.OnLogAdded -= HandleLogAdded;
    }

    void HandleLogAdded(string _)
    {
        RefreshLogs();
    }

    void RefreshLogs()
    {
        if (!logsText || CyberLabCampaignManager.I == null) return;

        var sb = new StringBuilder();
        foreach (var line in CyberLabCampaignManager.I.GetLogs())
            sb.AppendLine(line);

        logsText.text = sb.ToString();
    }

    void ShowOnly(GameObject panel)
    {
        if (panelHome) panelHome.SetActive(panel == panelHome);
        if (panelLogs) panelLogs.SetActive(panel == panelLogs);
        if (panelCameras) panelCameras.SetActive(panel == panelCameras);
        if (panelConsole) panelConsole.SetActive(panel == panelConsole);
    }

    public void OpenLogs()
    {
        CyberLabCampaignManager.I?.AddLog("Opened Logs screen.");
        ShowOnly(panelLogs);
        RefreshLogs();
    }

    public void OpenCameras()
    {
        CyberLabCampaignManager.I?.AddLog("Opened Cameras screen.");
        ShowOnly(panelCameras);
        if (cameraFeedback) cameraFeedback.text = "Tip: Disable only after console access.";
    }

    public void OpenConsole()
    {
        CyberLabCampaignManager.I?.AddLog("Opened Console Access screen.");
        ShowOnly(panelConsole);
        UpdateConsoleFeedback();
    }

    void AttemptLogin()
    {
        if (!act1Logic) return;

        string selectedId = GetSelectedCredentialId();
        bool ok = act1Logic.TryLogin(selectedId);

        if (consoleFeedback)
        {
            if (ok) consoleFeedback.text = "Access granted. You can now control CCTV.";
            else UpdateConsoleFeedback();
        }
    }

    void UpdateConsoleFeedback()
    {
        if (!consoleFeedback || !act1Logic) return;

        if (act1Logic.IsLocked)
            consoleFeedback.text = $"LOCKED: Wait {Mathf.CeilToInt(act1Logic.LockTimeRemaining)}s";
        else
            consoleFeedback.text = $"Attempts: {act1Logic.Attempts}/{act1Logic.maxAttemptsBeforeLockout}";
    }

    void DisableCameras()
    {
        if (!act1Logic) return;
        act1Logic.DisableCameras();

        if (cameraFeedback && CyberLabCampaignManager.I != null)
            cameraFeedback.text = CyberLabCampaignManager.I.camerasDisabled ? "CCTV: NO SIGNAL" : "CCTV: ACTIVE";
    }
  


    string GetSelectedCredentialId()
    {
        // Safe: dropdown option text becomes the ID.
        // Example dropdown options: "GUEST_BAD", "ADMIN_OK", "TEMP_BAD"
        if (!credentialDropdown) return "";
        return credentialDropdown.options[credentialDropdown.value].text.Trim();
    }
}
