#if CMPSETUP_COMPLETE
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using Fusion;

namespace AvocadoShark
{
    public class RoomEntry : MonoBehaviour
    {
        [Header("Joining")] public Button joinButton;
        [HideInInspector] public string password = null;
        public CanvasGroup canvasGroup;
        public float lerpSpeed = 0.5f; // Speed for fade in. Adjust as needed.
        public int currentPlayers, maxPlayers;
        public SessionInfo sessionInfo;


        public Color flashColor = Color.red;
        public float flashDuration = 1.0f;

        public TextMeshProUGUI roomName, playerCount;
        public FusionConnection fusionConnectionRef;

        [Header("Password")] [SerializeField] private GameObject passwordPanel;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button acceptPasswordbutton;

        private Color playerCountColor, passwordInputColor;

        Coroutine RoomIsFullIndicationRoutine, WrongPasswordIndicationRoutine;

        public void Start()
        {
            playerCountColor = playerCount.color;
            passwordInputColor = passwordInput.textComponent.color;
        }

        public void Init(SessionInfo session,FusionConnection fusionConnection)
        {
            transform.localScale = Vector3.one;
            session.Properties.TryGetValue("password", out SessionProperty passwordProperty);
            fusionConnection.OnDestroyRoomEntries += DestroyEntry;
            sessionInfo = session;
            roomName.text = session.Name;
            currentPlayers = session.PlayerCount;
            maxPlayers = session.MaxPlayers;
            playerCount.text = session.PlayerCount + "/" + session.MaxPlayers;
            joinButton.interactable = session.IsOpen;
            fusionConnectionRef = fusionConnection;
            if (passwordProperty == null) 
                return;
            Debug.Log(passwordProperty);
            password = passwordProperty;
        }

        public void UpdateEntry(SessionInfo session,FusionConnection fusionConnection)
        {
            session.Properties.TryGetValue("password", out SessionProperty passwordProperty);
            sessionInfo = session;
            roomName.text = session.Name;
            currentPlayers = session.PlayerCount;
            maxPlayers = session.MaxPlayers;
            playerCount.text = session.PlayerCount + "/" + session.MaxPlayers;
            joinButton.interactable = session.IsOpen;
            fusionConnectionRef = fusionConnection;
            if (passwordProperty != null)
            {
                password = passwordProperty;
            }
        }

        public void JoinRoom()
        {
            Debug.Log($"Password at Room entry {password}");

            if (currentPlayers >= maxPlayers)
            {
                RoomIsFullIndication();
                return;
            }
            PlayerPrefs.SetInt("has_pass", 0);
            if (string.IsNullOrEmpty(password))
            {
                fusionConnectionRef.loadingScreenScript.gameObject.SetActive(true);
               // Invoke(nameof(ContinueJoinRoom), fusionConnectionRef.loadingScreenScript.lerpSpeed);
               ContinueJoinRoom();
            }
            else
            {
                EnablePasswordPanel();
                joinButton.gameObject.SetActive(false);
            }
        }

        public void AcceptPassword()
        {
            if (currentPlayers >= maxPlayers)
            {
                RoomIsFullIndication();
                DisablePasswordPanel();
                joinButton.gameObject.SetActive(true);
                return;
            }

            if (passwordInput.text == password)
            {
                fusionConnectionRef.loadingScreenScript.gameObject.SetActive(true);
                PlayerPrefs.SetInt("has_pass", 1);
                ContinueJoinRoom();
            }
            else
            {
                WrongPasswordIndication();
            }
        }

        
        private void ContinueJoinRoom()
        {
            FusionConnection.Instance.JoinRoom(sessionInfo.Name, sessionInfo.MaxPlayers, password);

        }

        private void EnablePasswordPanel()
        {
            SetTryingToJoinRoom();
            passwordPanel.SetActive(true);
        }

        private void DisablePasswordPanel()
        {
            SetQuitTryingToJoinRoom();
            passwordPanel.SetActive(false);
        }

        public void RoomIsFullIndication()
        {
            if (RoomIsFullIndicationRoutine != null)
            {
                StopCoroutine(RoomIsFullIndicationRoutine);
                playerCount.color = playerCountColor;
            }

            RoomIsFullIndicationRoutine = StartCoroutine(FlashPlayerNumberText());
        }

        public void WrongPasswordIndication()
        {
            if (WrongPasswordIndicationRoutine != null)
            {
                StopCoroutine(WrongPasswordIndicationRoutine);
                passwordInput.textComponent.color = passwordInputColor;
            }

            WrongPasswordIndicationRoutine = StartCoroutine(FlashPasswordText());
        }

        public void DestroyEntry()
        {
            FusionConnection.Instance.OnDestroyRoomEntries -= DestroyEntry;
            Destroy(gameObject);
        }

        IEnumerator FlashPlayerNumberText()
        {
            playerCount.color = flashColor;
            yield return new WaitForSeconds(flashDuration / 2);

            playerCount.color = playerCountColor;
            yield return new WaitForSeconds(flashDuration / 2);
        }

        private IEnumerator FlashPasswordText()
        {
            passwordInput.textComponent.color = flashColor;
            yield return new WaitForSeconds(flashDuration / 2);

            passwordInput.textComponent.color = passwordInputColor;
            yield return new WaitForSeconds(flashDuration / 2);
        }

        public void OnDisable()
        {
            joinButton.onClick.RemoveListener(JoinRoom);
        }

        public void SetTryingToJoinRoom()
        {
            FusionConnection.Instance.SetCurrentEntryBeingEdited(this);
            FusionConnection.Instance.OnDestroyRoomEntries -= DestroyEntry;
        }

        public void SetQuitTryingToJoinRoom()
        {
            FusionConnection.Instance.ResetCurrentEntryBeingEdited(this);
        }
    }
}
#endif