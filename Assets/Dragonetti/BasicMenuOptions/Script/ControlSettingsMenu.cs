using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BasicMenuOptions{
    
public class ControlSettingsMenu : MonoBehaviour
{

    [Header("Mouse Sensitivity Slider")]
    [Tooltip("Audio Mixer"), SerializeField]
    private Slider sensitivitySlider;

    private float sensitivity = 1.0f;

    void Start()
    {
        UpdateAll();
    }

    public void GetSliderValue(float sliderValue)
    {
        sensitivity = sliderValue;
    }

    public void SaveMenuVariables()
    {
        PlayerPrefs.SetInt("SaveControls", 0);
        PlayerPrefs.SetFloat("sensitivityValue", sensitivitySlider.value);
    }


    public void UpdateAll()
    {
        if (PlayerPrefs.HasKey("SaveControls"))
        {
            sensitivitySlider.value = PlayerPrefs.GetFloat("sensitivityValue");
        }
    }
    }
}
