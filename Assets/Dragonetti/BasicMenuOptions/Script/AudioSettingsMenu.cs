using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace BasicMenuOptions
{

    public class AudioSettingsMenu : MonoBehaviour
    {

        [Header("Audio Settings")]
        [Tooltip("Audio Mixer"), SerializeField]
        public AudioMixer audioMixer;

        [Header("Sliders")]
        [SerializeField]
        private Slider mainSlider, musicSlider, fxSlider;

        void Start()
        {
            UpdateAll();
        }

        public void MainVolumeSlider(float sliderValue)
        {
            audioMixer.SetFloat("Master", Mathf.Log(sliderValue) * 20);
        }

        public void MusicVolumeSlider(float sliderValue)
        {
            audioMixer.SetFloat("Music", Mathf.Log(sliderValue) * 20);
        }

        public void SfxVolumeSlider(float sliderValue)
        {
            audioMixer.SetFloat("Sfx", Mathf.Log(sliderValue) * 20);
        }

        public void SaveMenuVariables() // Save to PlayerPrefs
        {
            PlayerPrefs.SetInt("SaveAudio", 0);
            PlayerPrefs.SetFloat("mainSliderValue", mainSlider.value);
            PlayerPrefs.SetFloat("musicSliderValue", musicSlider.value);
            PlayerPrefs.SetFloat("sfxSliderValue", fxSlider.value);
        }

        public void UpdateAll()
        {
            if (PlayerPrefs.HasKey("SaveAudio"))
            {
                mainSlider.value = PlayerPrefs.GetFloat("mainSliderValue");
                musicSlider.value = PlayerPrefs.GetFloat("musicSliderValue");
                fxSlider.value = PlayerPrefs.GetFloat("sfxSliderValue");

            }
        }
    }
}
