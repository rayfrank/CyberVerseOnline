#if CMPSETUP_COMPLETE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ChatSystem : MonoBehaviour
{
    [SerializeField] private ChatItem chatPrefab;
    [SerializeField] private GameObject  chatContainer;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private TextMeshProUGUI chatLimitDisplay;
    [SerializeField] private ScrollRect scrollRect;
    public static ChatSystem Instance = null;
    private ChatPlayer _chatPlayer;

    private bool _hasUserScroll;

    private void Awake()
    {
        Instance = this;
        chatInput.onFocusSelectAll = false;
        scrollRect.onValueChanged.AddListener((x) =>
        {
            _hasUserScroll = true;
        });
    }

    private void OnEnable()
    {
        chatInput.onSelect.AddListener(InputInFocus);
        chatInput.onDeselect.AddListener(InputLostFocus);
        chatInput.onSubmit.AddListener(InputSubmit);
        chatInput.onValueChanged.AddListener(CharacterCountUpdate);
    }

    private void OnDisable()
    {
        chatInput.onSelect.RemoveListener(InputInFocus);
        chatInput.onDeselect.RemoveListener(InputLostFocus);
        chatInput.onSubmit.RemoveListener(InputSubmit);
        chatInput.onValueChanged.RemoveListener(CharacterCountUpdate);
    }

    public void SetChatPlayer(ChatPlayer chatPlayer)
    {
        _chatPlayer = chatPlayer;
    }

    public void AddChatEntry(bool isLeft,Chat chat)
    {
        var go = Instantiate(chatPrefab, chatContainer.transform);
        go.Init(isLeft,chat);
        if (_hasUserScroll && !(scrollRect.verticalNormalizedPosition <= 0.1f))
            return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void InputInFocus(string text)
    {
        chatInput.MoveTextEnd(false);
        _chatPlayer.SetPlayerIsWriting(true);
    }

    public void InputLostFocus(string text)
    {
        _chatPlayer.SetPlayerIsWriting(false);
    }


    public void InputSubmit(string text)
    {
        if (text != "")
        {
            chatInput.ActivateInputField();
            var trimmedText = text.Trim();
            var newChat = new Chat(_chatPlayer.playerName, trimmedText);
            chatInput.text = "";
            _chatPlayer.SendChat(newChat);
        }
    }

    public void CharacterCountUpdate(string text)
    {
        chatLimitDisplay.text = text.Count() == chatInput.characterLimit
            ? $"<color=#D96222>{text.Count()}/{chatInput.characterLimit}</color>"
            : $"{text.Count()}/{chatInput.characterLimit}";
    }
}
#endif