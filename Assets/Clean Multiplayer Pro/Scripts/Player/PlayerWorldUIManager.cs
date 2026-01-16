#if CMPSETUP_COMPLETE
using Fusion;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using System;

public class PlayerWorldUIManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerVoiceUI;

    [SerializeField] private Vector2 backgroundSizeOffset;

    [SerializeField] private GameObject ChatBox;

    [SerializeField] private RectTransform backgroundRect;

    [SerializeField] private TextMeshProUGUI chatUI;

    [SerializeField] private ResizeBackgroundToText resizeBackgroundToText;

    [SerializeField] private int displayTime;

    [SerializeField] Canvas canvas;

    private readonly Queue<string> _chatQueue = new Queue<string>();
    //private bool isDisplaying = false;

    public Action<bool> OnSpeaking;

    private Task _chatTask;

    private ChangeDetector _speakingChangeDetector;
    [Networked] public NetworkBool IsSpeaking { get; set; }

    private bool _isShowingChat = false;

    public override void Spawned()
    {
        _speakingChangeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        foreach (var change in _speakingChangeDetector.DetectChanges(this, out var previousBuffer,
                     out var currentBuffer))
        {
            switch (change)
            {
                case nameof(IsSpeaking):
                    var reader = GetPropertyReader<NetworkBool>(nameof(IsSpeaking));
                    var (previous, current) = reader.Read(previousBuffer, currentBuffer);
                    OnIsSpeaking(previous, current);
                    break;
            }
        }
    }

    // Method to add a message to the chat queue
    public void QueueChat(Chat chat)
    {
        Debug.Log("Queueing chat");
        _chatQueue.Enqueue($"{chat.Message}");
        if (!_isShowingChat)
        {
            ShowChat();
        }
    }

    private async void ShowChat()
    {
        while (_chatQueue.Count > 0)
        {
            _isShowingChat = true;
            ChatBox.SetActive(true);
            chatUI.SetText(_chatQueue.Dequeue());
            chatUI.ForceMeshUpdate();
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(chatUI.textBounds.size.x + backgroundSizeOffset.x,
                chatUI.textBounds.size.y + backgroundSizeOffset.y);
            backgroundRect.anchoredPosition =
                new Vector2(backgroundRect.anchoredPosition.x, backgroundRect.sizeDelta.y / 2);
            await Task.Delay(5000);
            ChatBox.SetActive(false);
        }

        _isShowingChat = false;
    }

    public void SetIsSpeaking(bool value)
    {
        IsSpeaking = value;
    }

    public void SetPlayerVoiceUI(bool value)
    {
        if (!Object.HasStateAuthority)
            playerVoiceUI.SetActive(value);
    }

    public void OnIsSpeaking(NetworkBool previous, NetworkBool current)
    {
        OnSpeaking?.Invoke(current);
        SetPlayerVoiceUI(current);
    }
}
#endif