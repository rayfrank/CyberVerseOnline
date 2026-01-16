using System;
using System.Collections.Generic;
using UnityEngine;

public class CyberLabCampaignManager : MonoBehaviour
{
    public static CyberLabCampaignManager I { get; private set; }

    [Header("Campaign")]
    public int score = 0;

    [Header("State")]
    public bool act1ConsoleAccessGranted = false;
    public bool camerasDisabled = false;

    private readonly List<string> logs = new List<string>();

    public event Action<string> OnLogAdded;
    public event Action<int> OnScoreChanged;
    public event Action<bool> OnCamerasDisabledChanged;
    public event Action<bool> OnConsoleAccessChanged;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        // DontDestroyOnLoad(gameObject); // enable later if you have multiple scenes
        AddLog("Campaign started. Act 1: Camera Console.");
    }

    public IReadOnlyList<string> GetLogs() => logs;

    public void AddLog(string message)
    {
        string stamp = DateTime.Now.ToString("HH:mm:ss");
        string line = $"[{stamp}] {message}";
        logs.Add(line);
        Debug.Log(line);
        OnLogAdded?.Invoke(line);
    }

    public void AddScore(int delta, string reason = null)
    {
        score += delta;
        OnScoreChanged?.Invoke(score);
        if (!string.IsNullOrEmpty(reason))
            AddLog($"Score {(delta >= 0 ? "+" : "")}{delta}: {reason}");
    }

    public void SetCamerasDisabled(bool disabled)
    {
        camerasDisabled = disabled;
        OnCamerasDisabledChanged?.Invoke(disabled);
        AddLog(disabled ? "CCTV feeds disabled (simulation)." : "CCTV feeds restored.");
    }

    public void SetConsoleAccessGranted(bool granted)
    {
        act1ConsoleAccessGranted = granted;
        OnConsoleAccessChanged?.Invoke(granted);
        AddLog(granted ? "Console access granted." : "Console access revoked.");
    }

}
