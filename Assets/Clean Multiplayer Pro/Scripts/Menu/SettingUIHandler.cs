#if CMPSETUP_COMPLETE
using System.Collections.Generic;
using AvocadoShark;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingUIHandler : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameSetting gameSetting;
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TextMeshProUGUI sliderValue;
    [SerializeField] private List<GameObject> otherUIScreenGameObjects;

    [Header("KeyMappings")] [SerializeField]
    private List<PlayerInputRebinding> allPlayerInputRebinding;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        SaveBindings();
    }

    private void Initialize()
    {
        LoadBindings();
        soundToggle.isOn = gameSetting.SettingData.sound;
        sensitivitySlider.value = gameSetting.SettingData.lookSensitivity;
        sliderValue.text = sensitivitySlider.value.ToString("0.##");
        soundToggle.onValueChanged.AddListener(OnSoundToggle);
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
    }

    public void ShowSettingsPanel()
    {
        foreach (var playerInputRebinding in allPlayerInputRebinding)
        {
            playerInputRebinding.Initialize(this);
        }

        foreach (var obj in otherUIScreenGameObjects)
            obj.SetActive(false);
        
        settingsPanel.SetActive(true);
        playerInput.enabled = true;
    }

    public void BackToMenu()
    {
        settingsPanel.SetActive(false);
        foreach (var obj in otherUIScreenGameObjects)
            obj.SetActive(true);
        playerInput.enabled = false;
    }

    private void OnSoundToggle(bool isOn)
    {
        gameSetting.SettingData.sound = isOn;
        gameSetting.SaveSettings();
    }

    private void OnSensitivityChanged(float value)
    {
        gameSetting.SettingData.lookSensitivity = value;
        sliderValue.text = value.ToString("0.##");
        gameSetting.SaveSettings();
    }

    public void SaveBindings()
    {
        var bindings = playerInput.actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("PlayerBindings", bindings);
    }

    private void LoadBindings()
    {
        var bindings = PlayerPrefs.GetString("PlayerBindings", string.Empty);
        if (!string.IsNullOrEmpty(bindings))
        {
            playerInput.actions.LoadBindingOverridesFromJson(bindings);
        }
    }
}
#endif