using UnityEngine;

public class Act1ConsoleLogic : MonoBehaviour
{
    [Header("References")]
    public CctvWallController cctvWall;

    [Header("Login simulation (safe)")]
    public int maxAttemptsBeforeLockout = 3;
    public float lockoutSeconds = 10f;
    public string correctCredentialId = "ADMIN_OK";

    private int attempts;
    private bool locked;
    private float lockTimer;

    public bool ConsoleAccessGranted { get; private set; }
    public bool IsLocked => locked;
    public int Attempts => attempts;
    public float LockTimeRemaining => Mathf.Max(0f, lockTimer);

    void Update()
    {
        if (!locked) return;

        lockTimer -= Time.deltaTime;
        if (lockTimer <= 0f)
        {
            locked = false;
            attempts = 0;
            CyberLabCampaignManager.I?.AddLog("Lockout ended. Try again carefully.");
        }
    }

    public bool TryLogin(string playerCredentialId)
    {
        if (locked)
        {
            CyberLabCampaignManager.I?.AddLog($"Login blocked: locked ({Mathf.CeilToInt(lockTimer)}s left).");
            return false;
        }

        attempts++;

        if (playerCredentialId == correctCredentialId)
        {
            ConsoleAccessGranted = true;
            CyberLabCampaignManager.I?.SetConsoleAccessGranted(true);
            CyberLabCampaignManager.I?.AddScore(+20, "Console access granted");
            return true;
        }

        CyberLabCampaignManager.I?.AddLog($"Access denied. Attempt {attempts}/{maxAttemptsBeforeLockout}.");
        CyberLabCampaignManager.I?.AddScore(-2, "Failed login attempt");

        if (attempts >= maxAttemptsBeforeLockout)
        {
            locked = true;
            lockTimer = lockoutSeconds;
            CyberLabCampaignManager.I?.AddLog("ALERT: Too many failed attempts. Account lockout triggered.");
            CyberLabCampaignManager.I?.AddScore(-10, "Triggered lockout");
        }

        return false;
    }

    public void DisableCameras()
    {
        if (!ConsoleAccessGranted)
        {
            CyberLabCampaignManager.I?.AddLog("Denied: You must gain console access first.");
            CyberLabCampaignManager.I?.AddScore(-1, "Tried to disable cameras without access");
            return;
        }

        CyberLabCampaignManager.I?.SetCamerasDisabled(true);
        CyberLabCampaignManager.I?.AddScore(+15, "Disabled cameras (simulation)");

        if (cctvWall) cctvWall.DisableAll();
        else CyberLabCampaignManager.I?.AddLog("WARNING: No CctvWallController assigned.");
    }

}
