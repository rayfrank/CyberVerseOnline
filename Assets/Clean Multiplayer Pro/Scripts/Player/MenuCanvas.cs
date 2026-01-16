#if CMPSETUP_COMPLETE
using AvocadoShark;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuCanvas : MonoBehaviour
{
    [SerializeField] private GameObject RoomCreationPanel;
    [SerializeField] private Toggle passwordToggle;
    [SerializeField] private Slider slider;
    [SerializeField] private Button refreshRooms;
    [SerializeField] private TextMeshProUGUI roomCountText, nameInputFieldLimitText, passwordInputFieldLimitText;
    public bool IsPasswordEnabled { private set; get; } = false;

    [SerializeField] private TMP_InputField nameInputField, passwordInputField;

    private void Awake()
    {
        slider.onValueChanged.AddListener(OnSliderValueChange);
        passwordInputField.interactable = IsPasswordEnabled;
        passwordToggle.isOn = IsPasswordEnabled;
        passwordToggle.onValueChanged.AddListener(TogglePassword);
        nameInputField.onValueChanged.AddListener(NameInputUpdate);
        passwordInputField.onValueChanged.AddListener(PasswordInputUpdate);
    }

    private void TogglePassword(bool value)
    {
        IsPasswordEnabled = value;
        passwordInputField.interactable = value;
    }

    public void RefreshRooms()
    {
        FusionConnection.Instance.RefreshRoomList();
    }

    public string GetRoomName()
    {
        return nameInputField.text;
    }

    public string GetPassword()
    {
        return passwordInputField.text;
    }

    public int GetMaxPlayers()
    {
        return (int)slider.value;
    }

    private void OnSliderValueChange(float value)
    {
        roomCountText.text = Mathf.RoundToInt(value).ToString();
    }

    public void PasswordInputUpdate(string text)
    {
        passwordInputFieldLimitText.text = text.Count() == passwordInputField.characterLimit
            ? $"<color=#D96222>{text.Count()}/{passwordInputField.characterLimit}</color>"
            : $"{text.Count()}/{passwordInputField.characterLimit}";
    }

    public void NameInputUpdate(string text)
    {
        nameInputFieldLimitText.text = text.Count() == nameInputField.characterLimit
            ? $"<color=#D96222>{text.Count()}/{nameInputField.characterLimit}</color>"
            : $"{text.Count()}/{nameInputField.characterLimit}";
    }
}
#endif