#if CMPSETUP_COMPLETE
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace AvocadoShark
{
    public class PlayerUIEntry : MonoBehaviour
    {

        public string playerName;

        public Button voteKickButton;

        public GameObject voiceUI;

        public TextMeshProUGUI playerNameUI;

        public PlayerStats playerStats;


        public UnityAction<PlayerStats> OnVoteKick;

        public void Start()
        {
            playerNameUI.text = playerName;
            playerStats.OnSpeaking += ToggleVoiceUI;
            if (!playerStats.HasStateAuthority)
            {
                // If we are not local player
                if (SessionPlayers.instance.activePlayers.Count <= 2)
                {
                    AssignActionToButton();
                    ToggleVoteKickButton(false);
                }
                else
                    AssignActionToButton();
            }
            else
            {
                // If we are local player
                ToggleVoteKickButton(false);
            }
        }

        public void AssignActionToButton()
        {
            voteKickButton.onClick.AddListener(() => { OnVoteKick.Invoke(playerStats); });
        }

        public void ToggleVoiceUI(bool value)
        {
            voiceUI.gameObject.SetActive(value);
        }

        public void ToggleVoteKickButton(bool value)
        {
            voteKickButton.gameObject.SetActive(value);
        }

        private void OnDestroy()
        {

        }
    }
}
#endif
