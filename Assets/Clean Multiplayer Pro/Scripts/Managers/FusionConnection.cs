#if CMPSETUP_COMPLETE
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using StarterAssets;
using Fusion.Photon.Realtime;
using UnityEngine.Serialization;

namespace AvocadoShark
{
    public class FusionConnection : MonoBehaviour, INetworkRunnerCallbacks
    {
        #region ThingsAddedByMe

        public List<PlayerRef> playersInSession = new List<PlayerRef>();

        #endregion

        public RoomEntry CurrentEntryBeingEdited;
        public Action OnDestroyRoomEntries;

        public static FusionConnection Instance;

        [SerializeField] private NetworkRunner runnerPrefab;
        public NetworkRunner Runner { get; private set; }

        public bool hasEnteredGameScene = false;

        [Header("Player 1")]
        [SerializeField] public GameObject playerPrefabYellow;
        [Header("Player 2")]
        [SerializeField] public GameObject playerPrefabRed;
        public CharacterSO characterScriptableObject;

        [Header("Name Entry")]
        public GameObject mainObject;
        public Button submitButton;
        public TMP_InputField nameField;
        public GameObject characterselectionobject;

        [Header("Room List")]
        public RoomEntry roomEntryPrefab;
        public GameObject roomListObject;
        public Transform content;
        public Button createRoomButton;
        public TextMeshProUGUI NoRoomsText;
        public TMP_InputField room_search;

        [Header("Room List Refresh (s)")]
        [SerializeField] private float refreshInterval = 2f;

        [Header("Player Spawn Location")]
        [SerializeField] public bool UseCustomLocation;
        [SerializeField] public Vector3 CustomLocation;

        private FusionVoiceClient _fvc;
        private Recorder _recorder;
        private VoiceManager _voiceManager;

        [Header("Loading Screen")]
        public LoadingScreen loadingScreenScript;

        [Header("Popups")]
        public PopUp popup;

        [Header("UI")]
        [SerializeField] private MenuCanvas menuCanvas;

        private bool initialRoomListPopulated = false;
        private List<SessionInfo> _sessionList = new List<SessionInfo>();
        private List<RoomEntry> _roomEntryList = new List<RoomEntry>();

        [HideInInspector] public bool isConnected = false;
        [HideInInspector] public string _playerName = null;
        [HideInInspector] public int nRooms = 0;
        [HideInInspector] public int nPPLOnline = 0;

        public TMP_Dropdown region_select;
        public Button backButton;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            room_search.onValueChanged.AddListener(OnSearchTextValueChange);

#if UNITY_2022_3_OR_NEWER
            Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
#else
            Application.targetFrameRate = Screen.currentResolution.refreshRate;
#endif

            int region_n = PlayerPrefs.GetInt("region");
            string region = null;
            if (region_n == 0)
            {
                region = "";
            }
            else if (region_n == 1)
            {
                region = "asia";
            }
            else if (region_n == 2)
            {
                region = "eu";
            }
            else if (region_n == 3)
            {
                region = "jp";
            }
            else if (region_n == 4)
            {
                region = "kr";
            }
            else if (region_n == 5)
            {
                region = "us";
            }
            region_select.value = region_n;

            PhotonAppSettings settings = Resources.Load<PhotonAppSettings>("PhotonAppSettings");
            settings.AppSettings.FixedRegion = region;
        }

        public void ChangeRegion(int regionNum)
        {
            string region = null;
            var settings = Resources.Load<PhotonAppSettings>("PhotonAppSettings");
            region = regionNum switch
            {
                0 => "",
                1 => "asia",
                2 => "eu",
                3 => "jp",
                4 => "kr",
                5 => "us",
                _ => null
            };
            settings.AppSettings.FixedRegion = region;
            PlayerPrefs.SetInt("region", regionNum);
        }

        public void RefreshRoomList()
        {
            InitialRoomListSetup();
        }

        private IEnumerator AutoRefreshRoomList()
        {
            while (true)
            {
                RefreshRoomList();
                yield return new WaitForSeconds(refreshInterval);
            }
        }

        public void OnSearchTextValueChange(string value)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
            {
                foreach (var i in _roomEntryList)
                {
                    i.gameObject.SetActive(true);
                }
            }

            foreach (var i in _roomEntryList)
            {
                if (value != null && i.roomName.text.Contains(value))
                    i.gameObject.SetActive(true);
                else
                    i.gameObject.SetActive(false);
            }
        }

        public void CreateRoom()
        {
            PlayerPrefs.SetInt("has_pass", 0);
            loadingScreenScript.gameObject.SetActive(true);
            Invoke(nameof(ContinueCreateRoom), loadingScreenScript.lerpSpeed);
        }

        private void ContinueCreateRoom()
        {
            string sessionName = null;
            string sessionPassword = null;
            int maxPlayers = 2;
            if (IsRoomNameValid())
            {
                sessionName = menuCanvas.GetRoomName();
                sessionPassword = menuCanvas.GetPassword();
                maxPlayers = menuCanvas.GetMaxPlayers();
            }
            else
            {
                int randomInt = UnityEngine.Random.Range(1000, 9999);
                sessionPassword = menuCanvas.GetPassword();
                maxPlayers = menuCanvas.GetMaxPlayers();
                sessionName = "Room-" + randomInt;
            }

            Debug.Log($"Session name is {sessionName}");
            Debug.Log($"maxPlayers is {maxPlayers}");

            if (menuCanvas.IsPasswordEnabled)
            {
                PlayerPrefs.SetInt("has_pass", 1);
                JoinRoom(sessionName, maxPlayers, sessionPassword);
            }
            else
            {
                JoinRoom(sessionName, maxPlayers, string.Empty);
            }

            StopCoroutine(AutoRefreshRoomList());
        }

        private bool IsRoomNameValid()
        {
            return menuCanvas.GetRoomName().Length != 0;
        }

        public bool TryGetLocalPlayerComponent<T>(out T component)
        {
            var localPlayer = Runner.GetPlayerObject(Runner.LocalPlayer);
            if (localPlayer == null)
            {
                component = default;
                return false;
            }
            component = localPlayer.GetComponent<T>();
            return component != null;
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (initialRoomListPopulated == false)
            {
                //StartCoroutine(AutoRefreshRoomList());
                loadingScreenScript.FadeOutAndDisable();
            }

            _sessionList = sessionList;
            nRooms = sessionList.Count;

            nPPLOnline = 0;
            foreach (var session in sessionList)
            {
                nPPLOnline += session.PlayerCount;
            }

            RefreshRoomList();
        }

        private void InitialRoomListSetup()
        {
            if (roomListObject == null)
                return;
            initialRoomListPopulated = true;
            roomListObject.SetActive(true);

            OnDestroyRoomEntries?.Invoke();

            foreach (SessionInfo session in _sessionList)
            {
                if (CurrentEntryBeingEdited != null)
                {
                    if (session.Name == CurrentEntryBeingEdited.sessionInfo.Name)
                    {
                        UpdateCurrentEntryBeingEdited(session);
                        continue;
                    }
                }

                RoomEntry entryScript = Instantiate(roomEntryPrefab, content);
                entryScript.Init(session, this);
                _roomEntryList.Add(entryScript);
            }

            if (_sessionList.Count == 0)
            {
                NoRoomsText.gameObject.SetActive(true);
            }
            else
            {
                NoRoomsText.gameObject.SetActive(false);
            }
        }

        public void ConnectToRunner()
        {
            loadingScreenScript.gameObject.SetActive(true);
            Invoke(nameof(ContinueConnectToRunner), loadingScreenScript.lerpSpeed);
        }

        private void ContinueConnectToRunner()
        {
            _playerName = nameField.text;
            mainObject.SetActive(false);
            characterselectionobject.SetActive(false);
            SetUpComponents();
            Runner.JoinSessionLobby(SessionLobby.Shared);
        }

        private void SetUpComponents()
        {
            Runner = Instantiate(runnerPrefab);
            _fvc = Runner.GetComponent<FusionVoiceClient>();
            _recorder = Runner.GetComponentInChildren<Recorder>();
            _voiceManager = Runner.GetComponentInChildren<VoiceManager>();
            Runner.AddCallbacks(this);
        }

        // ==========================================================
        //        SINGLE JOINROOM FUNCTION (ALWAYS ENVIRONMENT 3)
        // ==========================================================
        public async void JoinRoom(string sessionName, int maxPlayers, string password = null)
        {
            string targetSceneName = "Environment 3";   // make sure this matches the scene asset name
            int buildIndex = -1;

            // Find "Environment 3" in Build Settings
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                if (sceneName == targetSceneName)
                {
                    buildIndex = i;
                    break;
                }
            }

            if (buildIndex < 0)
            {
                Debug.LogError($"[FusionConnection] Could not find scene '{targetSceneName}' in Build Settings. " +
                               "Add it to File > Build Settings.");
                return;
            }

            var sessionProperties = new Dictionary<string, SessionProperty>
            {
                { "password", password }
            };

            StopCoroutine(AutoRefreshRoomList());

            if (Runner == null)
            {
                SetUpComponents();
            }

            var result = await Runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                Scene = SceneRef.FromIndex(buildIndex),
                SessionProperties = sessionProperties,
                PlayerCount = maxPlayers
            });

            if (!result.Ok)
            {
                popup.ShowPopup(result.ShutdownReason.ToString());
            }
        }

        private void SpawnPlayerCharacter(NetworkRunner runner)
        {
            // Avoid double-spawn
            if (runner.GetPlayerObject(runner.LocalPlayer) != null)
            {
                Debug.Log("[FusionConnection] Player object already exists, not spawning again.");
                return;
            }

            var playerPrefab = characterScriptableObject.GetSelectedCharacter().character;
            var location = !UseCustomLocation
                ? new Vector3(UnityEngine.Random.Range(-7.6f, 14.2f), 0,
                    UnityEngine.Random.Range(-31.48f, -41.22f))
                : CustomLocation;

            Debug.Log($"[FusionConnection] Spawning player at {location}");

            var playerObject = runner.Spawn(playerPrefab, location);

            _voiceManager.Init(playerObject.GetComponent<StarterAssetsInputs>(),
                playerObject.GetComponent<PlayerWorldUIManager>());

            runner.SetPlayerObject(runner.LocalPlayer, playerObject.Object);
        }

        #region INetworkCallbacks

        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log("OnConnectedToServer");
            isConnected = true;
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            isConnected = false;
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            popup.ShowPopup(reason.ToString());
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token)
        {
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key,
            ArraySegment<byte> data)
        {
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            playersInSession.Add(player);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
        }

        // ✅ This now just spawns in the scene Fusion already loaded (Environment 3)
        public void OnSceneLoadDone(NetworkRunner runner)
        {
            Debug.Log("[FusionConnection] Scene Load Done.");

            if (hasEnteredGameScene)
                return;

            hasEnteredGameScene = true;

            // ✅ Spawn the player in the scene Fusion just loaded (Environment 3)
            SpawnPlayerCharacter(runner);

            // ✅ Unload the menu / lobby scene (the scene this script lives in)
            var menuScene = gameObject.scene;
            if (menuScene.IsValid())
            {
                Debug.Log("[FusionConnection] Unloading menu scene: " + menuScene.name);
                SceneManager.UnloadSceneAsync(menuScene);
            }
        }


        public void OnSceneLoadStart(NetworkRunner runner)
        {
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        #endregion

        public void Checkforname()
        {
            submitButton.interactable = !string.IsNullOrEmpty(nameField.text);
        }

        public void UpdateCurrentEntryBeingEdited(SessionInfo session)
        {
            CurrentEntryBeingEdited.UpdateEntry(session, this);
        }

        public void SetCurrentEntryBeingEdited(RoomEntry roomEntry)
        {
            if (CurrentEntryBeingEdited != null)
            {
                OnDestroyRoomEntries += CurrentEntryBeingEdited.DestroyEntry;
                CurrentEntryBeingEdited = roomEntry;
            }
            else
            {
                CurrentEntryBeingEdited = roomEntry;
            }
        }

        public void ResetCurrentEntryBeingEdited(RoomEntry roomEntry)
        {
            if (roomEntry == CurrentEntryBeingEdited)
            {
                OnDestroyRoomEntries += CurrentEntryBeingEdited.DestroyEntry;
                CurrentEntryBeingEdited = null;
            }
        }
    }
}
#endif
