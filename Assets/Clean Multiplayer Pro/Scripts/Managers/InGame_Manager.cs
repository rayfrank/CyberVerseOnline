#if CMPSETUP_COMPLETE
using Fusion;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using StarterAssets;
using UnityEngine.InputSystem;

namespace AvocadoShark
{
    public class InGame_Manager : NetworkBehaviour
    {
        public int environment1, environment2;
        public TextMeshProUGUI room_name;
        public TextMeshProUGUI players;
        public Image lock_image;
        public Sprite locked_sprite;
        public TextMeshProUGUI lock_status_text;
        public TextMeshProUGUI InfoText;
        [HideInInspector] public float deltaTime;
        private float fps;
        public int ping;
        public NetworkRunner runner;
        
        [Header("Game Pause")]
        [SerializeField] private GameObject pauseMenu;

        [SerializeField] private InputActionAsset inputActionAsset;
        private void Start()
        {
            if (PlayerPrefs.GetInt("has_pass") == 1)
            {
                lock_image.sprite = locked_sprite;
                lock_status_text.text = "private";
            }
            fps = 1.0f / Time.smoothDeltaTime;

            runner = NetworkRunner.GetRunnerForGameObject(gameObject);
        }
        public void LeaveRoom()
        {
            var fusionManager = FindFirstObjectByType<FusionConnection>();
            if (fusionManager != null)
            {
                Destroy(fusionManager);
            }
            Runner.Shutdown();
            SceneManager.LoadScene("Menu");
        }
        private float smoothedRTT = 0.0f;
        private void LateUpdate()
        { 
            if(!Object)
                return;
            room_name.text = Runner.SessionInfo.Name;
            players.text = Runner.SessionInfo.PlayerCount + "/" + Runner.SessionInfo.MaxPlayers;
            float newFPS = 1.0f / Time.smoothDeltaTime;
            fps = Mathf.Lerp(fps, newFPS, 0.005f);

            double rttInSeconds = runner.GetPlayerRtt(PlayerRef.None);
            int rttInMilliseconds = (int)(rttInSeconds * 1000);
            smoothedRTT = Mathf.Lerp(smoothedRTT, rttInMilliseconds, 0.005f);
            int ping = (int)smoothedRTT / 2;
            InfoText.text = "Ping: " + ping.ToString() + "\n" + "FPS: " + ((int)fps).ToString();
        }

        public void SwitchScene()
        {
            if (!HasStateAuthority)
            {
                SceneSwitchRpc();
                return;
            }
            if (!Runner.IsSceneAuthority) 
                return;
            //Assuming envir scene in additive mode is loaded at 1 index
            var environmentSceneIndex = 1;
            var environmentScene = SceneManager.GetSceneAt(environmentSceneIndex);
            print(environmentScene.name);
            var isEnvironment1 = environmentScene.buildIndex == environment1;
            var sceneToLoad = isEnvironment1 ? environment2 : environment1;
            
            Runner.LoadScene(SceneRef.FromIndex(sceneToLoad), LoadSceneMode.Additive);
            Runner.UnloadScene(SceneRef.FromIndex(environmentScene.buildIndex));
        }

        [Rpc(RpcSources.Proxies,RpcTargets.StateAuthority)]
        private void SceneSwitchRpc()
        {
            SwitchScene();
        }

        public void PauseGame()
        {
            if (FusionConnection.Instance.TryGetLocalPlayerComponent(out StarterAssetsInputs starterAssetsInputs))
            {
                starterAssetsInputs.DisablePlayerInput();
            }

            pauseMenu.SetActive(true);
            //inputActionAsset.Disable();
        }
        public void ResumeGame()
        {
            pauseMenu.SetActive(false);
            //inputActionAsset.Enable();
            if (FusionConnection.Instance.TryGetLocalPlayerComponent(out StarterAssetsInputs starterAssetsInputs))
            {
                starterAssetsInputs.EnablePlayerInput();
            }
        }
    }
}
#endif
