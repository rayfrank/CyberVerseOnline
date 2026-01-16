using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;


namespace BasicMenuOptions{
public class VideoSettingsMenu : MonoBehaviour {

    [SerializeField]
    private Text qualityText, antiAliasText, shadowText, textureText, windowsModeText, vsyncText, resolutionText;

    private int qualityValue, totalQuality, antialiasValue, shadowValue, textureQualityValue, windowsModeValue, fpsValue, vsyncValue, resolutionValue, defaultResolutionValue;

    private Resolution[] resolutions;

    private Resolution currentResolution;

    private List<Resolution> resolutionsList;




    void Start()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogWarning("Please add an Event System on your Scene");
        }
        LoadPrefs();     //Load all values from PlayerPreps.                                   
        UpdateAll();     

    }
    private void UpdateAll()
    {
        InstallResolution();
        InstallQuality();
        UpdateResolution();
        UpdateShadowResolution();
        UpdateQuality();
        UpdateVsync();
        UpdateWindowsMode();
        UpdateAntiAliasing();
        UpdateShadowResolution();
        UpdateTextureQuality();

    }
    public void SaveToPrefs() // Save to PlayerPrefs
    {
        PlayerPrefs.SetInt("Resolution", resolutionValue);
        PlayerPrefs.SetInt("Quality", qualityValue);
        PlayerPrefs.SetInt("AntiAliaser", antialiasValue);
        PlayerPrefs.SetInt("Shadow", shadowValue);
        PlayerPrefs.SetInt("Texture", textureQualityValue);
        PlayerPrefs.SetInt("windowMode", windowsModeValue);
        PlayerPrefs.SetInt("VSync", vsyncValue);
        PlayerPrefs.SetInt("SaveVideo", 0);

        UpdateAll();

    }
    public void LoadPrefs() //Load values from the PlayerPrefs.
    {
        if (PlayerPrefs.HasKey("SaveVideo"))
        {
            resolutionValue = PlayerPrefs.GetInt("Resolution");
            qualityValue = PlayerPrefs.GetInt("Quality");
            antialiasValue = PlayerPrefs.GetInt("AntiAliaser");
            shadowValue = PlayerPrefs.GetInt("Shadow");
            textureQualityValue = PlayerPrefs.GetInt("Texture");
            windowsModeValue = PlayerPrefs.GetInt("windowMode");
            vsyncValue = PlayerPrefs.GetInt("VSync");
        }

    }
    public void ResetToDefault() //Default Settings. You can change with your own wish.
    {

        qualityValue = 1;
        antialiasValue = 0;
        shadowValue = 0;
        textureQualityValue = 1;
        vsyncValue = 0;
        windowsModeValue = 1;
        resolutionValue = defaultResolutionValue;


        UpdateResolution();
        UpdateQuality();
        UpdateVsync();
        UpdateWindowsMode();
        UpdateAntiAliasing();
        UpdateShadowResolution();
        UpdateTextureQuality();
    }
    public void ChangeQualityUp() 
    {
        if (qualityValue >= totalQuality-1)
            qualityValue = 0;
        else
            qualityValue++;

        UpdateQuality();
    }
    public void ChangeQualityDown() 
    {
        if (qualityValue == 0)
            qualityValue = totalQuality-1;
        else
            qualityValue--;

        UpdateQuality();
    }
    private void UpdateQuality()
    {
        QualitySettings.SetQualityLevel(qualityValue, true);
        qualityText.text = QualitySettings.names[qualityValue];
    }
    public void InstallQuality()
    {
        totalQuality = QualitySettings.names.Length;
    }
    public void ChangeWindowsMode()
    {
        if (windowsModeValue == 0)
        {
            windowsModeValue = 1;
            windowsModeText.text = "Off";
        }
        else
        {
            windowsModeValue = 0;
            windowsModeText.text = "On";
        }
    }
    private void UpdateWindowsMode()
    {
        if (windowsModeValue == 0)
        {
            windowsModeValue = 0;
            windowsModeText.text = "On";
        }
        else
        {
            windowsModeValue = 1;
            windowsModeText.text = "Off";
        }

        if (PlayerPrefs.HasKey("SaveVideo"))
        {
            Screen.SetResolution(resolutionsList[resolutionValue].width, resolutionsList[resolutionValue].height, Convert.ToBoolean(windowsModeValue));
        }
    }
    public void ChangeVsync()
    {
        if (vsyncValue == 0)
        {
            vsyncValue = 1;
            vsyncText.text = "On";
        }
        else
        {
            vsyncValue = 0;
            vsyncText.text = "Off";
        }
    }
    private void UpdateVsync()
    {
        if (vsyncValue == 0)
        {
            vsyncValue = 0;
            vsyncText.text = "Off";
        }
        else
        {
            vsyncValue = 1;
            vsyncText.text = "On";
        }
    }
    public void ChangeAntiAliasingUp()
    {

        if (antialiasValue == 3)
            antialiasValue = 0;
        else
            antialiasValue++;

        UpdateAntiAliasing();
    }
    public void ChangeAntiAliasingDown()
    {
        if (antialiasValue == 0)
            antialiasValue = 3;
        else
            antialiasValue--;

        UpdateAntiAliasing();
    }
    private void UpdateAntiAliasing()
    {
        switch (antialiasValue)
        {
            case 0:
                QualitySettings.antiAliasing = 0;
                antiAliasText.text = "Off";

                break;
            case 1:
                QualitySettings.antiAliasing = 2;
                antiAliasText.text = "2x";

                break;
            case 2:
                QualitySettings.antiAliasing = 4;
                antiAliasText.text = "4x";

                break;
            case 3:
                QualitySettings.antiAliasing = 8;
                antiAliasText.text = "8x";
                break;

        }
    }
    public void ChangeShadowUp()
    {
        if (shadowValue == 3)
            shadowValue = 0;
        else
            shadowValue++;

        UpdateShadowResolution();
    }
    public void ChangeShadowDown()
    {
        if (shadowValue == 0)
            shadowValue = 3;
        else
            shadowValue--;

        UpdateShadowResolution();
    }
    private void UpdateShadowResolution()
    {
        switch (shadowValue)
        {
            case 0:
                QualitySettings.shadowResolution = ShadowResolution.Low;
                shadowText.text = "Low";
                break;
            case 1:
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                shadowText.text = "Medium";
                break;
            case 2:
                QualitySettings.shadowResolution = ShadowResolution.High;
                shadowText.text = "High";
                break;
            case 3:
                QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                shadowText.text = "VeryHigh";
                break;
        }
    }
    public void ChangeTextureQualityUp()
    {
        if (textureQualityValue == 3)
            textureQualityValue = 0;
        else
            textureQualityValue++;

        UpdateTextureQuality();
    }
    public void ChangeTextureQualityDown()
    {
        if (textureQualityValue == 0)
            textureQualityValue = 3;
        else
            textureQualityValue--;

        UpdateTextureQuality();
    }
    private void UpdateTextureQuality()
    {
        switch (textureQualityValue)
        {
            case 0:
                QualitySettings.globalTextureMipmapLimit = 3;
                textureText.text = "Eighth Resolution";
                break;
            case 1:
                QualitySettings.globalTextureMipmapLimit = 2;
                textureText.text = "Quarter Resolution";
                break;
            case 2:
                QualitySettings.globalTextureMipmapLimit = 1;
                textureText.text = "Half Resolution";
                break;
            case 3:
                QualitySettings.globalTextureMipmapLimit = 0;
                textureText.text = "Full Resolution";
                break;
        }
    }
    public void ChangeResolutionsUp()
    {
        if (resolutionValue >= resolutionsList.Count - 1)
            resolutionValue = 0;
        else
            resolutionValue++;
        
        resolutionText.text = resolutionsList[resolutionValue].width + " x " + resolutionsList[resolutionValue].height;
    }
    public void ChangeResolutionsDown()
    {
        if (resolutionValue <= 0)
            resolutionValue = resolutionsList.Count - 1;
        else
            resolutionValue--;

        resolutionText.text = resolutionsList[resolutionValue].width + " x " + resolutionsList[resolutionValue].height;
    }
    public void UpdateResolution()
    {
        if (PlayerPrefs.HasKey("SaveVideo"))
        {
            Screen.SetResolution(resolutionsList[resolutionValue].width, resolutionsList[resolutionValue].height, Convert.ToBoolean(windowsModeValue));
            resolutionText.text = resolutionsList[resolutionValue].width + " x " + resolutionsList[resolutionValue].height;
        }
        else
        {
            currentResolution = Screen.currentResolution;
            Screen.SetResolution(currentResolution.width,currentResolution.height,true);
            resolutionText.text = currentResolution.width + " x " + currentResolution.height;
        }
    }

    public void InstallResolution()         //Each monitor can have more than one same resolution. That's why we eliminate it.
    {
        int previousX = 0;
        int previousY = 0;

        resolutions = Screen.resolutions;
        resolutionsList = new List<Resolution>();

        for (int i = 0; i < resolutions.Length; i++)
        {

            if (resolutions[i].width == previousX && resolutions[i].height == previousY)
            {
                previousX = resolutions[i].width;
                previousY = resolutions[i].height;
            }
            else
            {
                if (resolutions[i].width == 1920 && resolutions[i].height == 1080)
                    defaultResolutionValue = i;

                previousX = resolutions[i].width;
                previousY = resolutions[i].height;
                resolutionsList.Add(resolutions[i]);
            }
        }
    }   
}
}
