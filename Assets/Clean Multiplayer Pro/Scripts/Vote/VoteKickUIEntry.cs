#if CMPSETUP_COMPLETE
using AvocadoShark;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VoteKickUIEntry : MonoBehaviour {

    [HideInInspector] public PlayerStats playerStats;
    [SerializeField] private GameObject buttonParent;
    [SerializeField] private Button positiveButton, negativeButton;
    [SerializeField] TextMeshProUGUI playerNameUI, voteUI, voteTimeUI;
    public int positiveVotes = 0, negativeVotes = 0, totalVotes;
    public Action<PlayerStats> OnPositiveButtonPressed, OnNegativeButtonPressed;

    private void Start() {
        positiveButton.onClick.AddListener(() => {
            OnPositiveButtonPressed?.Invoke(playerStats);
            buttonParent.SetActive(false);
        });
        negativeButton.onClick.AddListener(() => {
            OnNegativeButtonPressed?.Invoke(playerStats);
            buttonParent.SetActive(false);
        });
        playerStats.OnPositiveVotesChanged += RecievePositiveVotes;
        playerStats.OnNegativeVotesChanged += RecieveNegativeVotes;
        playerStats.OnVoteTimeUpdated += RecieveVoteTime;
        UpdateVoteUI();
    }


    public void RecievePositiveVotes(int votes) {
        positiveVotes = votes;
        UpdateVoteUI();
    }

    public void RecieveNegativeVotes(int votes) {
        Debug.Log("RecieveNegativeVotes");
        negativeVotes = votes;
        UpdateVoteUI();
    }
    public void RecieveVoteTime(int time)
    {
        voteTimeUI.text = time.ToString();
        UpdateVoteUI();
    }

    public void ToggleVoteButtons(bool value) {
        positiveButton.gameObject.SetActive(value);
        negativeButton.gameObject.SetActive(value);
    }   

    public void SetPlayerName(string playerName) {
        playerNameUI.text = "Kick: " + playerName + "?";
    }

    public void UpdateVoteUI() {
        voteUI.text = $"For: {positiveVotes}   Against: {negativeVotes}";
    }
}
#endif