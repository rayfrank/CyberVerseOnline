#if CMPSETUP_COMPLETE
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace AvocadoShark
{
    public class Menu : MonoBehaviour
    {
        [Header("Game Stats")]
        public TextMeshProUGUI Players_In_Rooms;
        public TextMeshProUGUI RoomsMade;
        [HideInInspector]
        public int characterchosen;
        public TextMeshProUGUI optionchosentext;
        [Header("Character Selection")]
        public CharacterSO charactersSO;
        public GameObject modelParent;
        private int _currentIndex;
        private GameObject _tempModel;
        private void Start()
        {
            _currentIndex = charactersSO.GetSelectedCharacterIndex;
            UpdateCharacter();
        }

        private void Update()
        {
            RoomsMade.text = "Total Rooms: " + FusionConnection.Instance.nRooms;
            Players_In_Rooms.text = "Players In Rooms: " + FusionConnection.Instance.nPPLOnline;
        }
        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
        public void OpenProVersion()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/slug/264984");
        }
        public void OpenSupport()
        {
            Application.OpenURL("https://discord.gg/mP4yfHxXPa");
        }
        public void OpenAvocadoShark()
        {
            Application.OpenURL("https://avocadoshark.com/");
        }
        public void OpenFusionDocumentation()
        {
            Application.OpenURL("https://doc.photonengine.com/fusion/current/fusion-intro");
        }
        public void Next()
        {
            if (charactersSO.characters.Count == 0)
                return; 
            _currentIndex = (_currentIndex + 1) % charactersSO.characters.Count;
            UpdateCharacter();
        }

        public void Previous()
        {
            if (charactersSO.characters.Count == 0) 
                return; 
            _currentIndex = (_currentIndex - 1 + charactersSO.characters.Count) % charactersSO.characters.Count;
            UpdateCharacter();
        }

        private void UpdateCharacter()
        {
            if(_tempModel!=null)
                Destroy(_tempModel);
            
            var character = charactersSO.characters[_currentIndex];
            _tempModel = Instantiate(character.displayModel, modelParent.transform);
            optionchosentext.text = $"Choose your character: \n <b>{character.characterName} Chosen</b>";
            charactersSO.SaveSelectedCharacter(character);
        }
    }
}
#endif