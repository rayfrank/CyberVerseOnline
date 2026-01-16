using System;
using UnityEngine;

namespace AvocadoShark
{
    [CreateAssetMenu(fileName = "GameSetting", menuName = "ScriptableObject/Game Setting")]
    public class GameSetting : ScriptableObject
    {
        [SerializeField] private SettingData settingData;
        private const string Key = "GameSettingData";

        public SettingData SettingData => LoadSettings();
        private SettingData LoadSettings()
        {
            if (PlayerPrefs.HasKey(Key))
                settingData = JsonUtility.FromJson<SettingData>(PlayerPrefs.GetString(Key));
            return settingData;
        }

        public void SaveSettings()
        {
            var json = JsonUtility.ToJson(settingData);
            PlayerPrefs.SetString(Key,json);
            PlayerPrefs.Save();
        }
    }

    [Serializable]
    public class SettingData
    {
        public bool sound;
        [Range(0.25f, 5f)] public float lookSensitivity;
    }
}