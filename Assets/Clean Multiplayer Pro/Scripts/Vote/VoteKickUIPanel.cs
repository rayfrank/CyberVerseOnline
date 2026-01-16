#if CMPSETUP_COMPLETE
using AvocadoShark;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoteKickUIPanel : MonoBehaviour {

    [SerializeField] private GameObject kickPrefab;
    public SessionPlayers sessionPlayersRef;

    private List<VoteKickUIEntry> voteKickList = new List<VoteKickUIEntry>();
    public string nameOfPlayerToVoteAgainst;
    public static VoteKickUIPanel Instance;
    private void Awake()
    {
        Instance = this;
        sessionPlayersRef.OnVoteKickInitiated += CreateKickInList;
        sessionPlayersRef.OnVoteKickFinished += RemoveKickFromList;
    }

    public void CreateKickInList(PlayerStats playerStats) {
        GameObject kickUiInstance = Instantiate(kickPrefab, transform);
        VoteKickUIEntry voteKickUIEntry = kickUiInstance.GetComponent<VoteKickUIEntry>();
        voteKickUIEntry.playerStats = playerStats;
        voteKickUIEntry.SetPlayerName(playerStats.PlayerName.ToString());
        nameOfPlayerToVoteAgainst = playerStats.PlayerName.Value;
        voteKickUIEntry.RecievePositiveVotes(playerStats.PositiveVotes);
        voteKickUIEntry.RecieveNegativeVotes(playerStats.NegativeVotes);
        voteKickUIEntry.OnPositiveButtonPressed += OnPositiveResponse;
        voteKickUIEntry.OnNegativeButtonPressed += OnNegativeResponse;
        voteKickUIEntry.ToggleVoteButtons(!(playerStats.isVoteInitiator ^ playerStats.Object.HasStateAuthority));
        voteKickList.Add(voteKickUIEntry);
    }

    public void RemoveKickFromList(PlayerStats playerStats) { 
        int deleteIndex = 0;
        for (int i = 0; i < voteKickList.Count; i++) {
            if (voteKickList[i].playerStats == playerStats) { 
                deleteIndex = i; 
                break; 
            }
        }
        Debug.Log(playerStats);
        Destroy(voteKickList[deleteIndex].gameObject);
        voteKickList.RemoveAt(deleteIndex);
    }

    public void OnPositiveResponse(PlayerStats playerStats) {
        Debug.Log("Positive Response");
        playerStats.AddPositiveVote();
    }

    public void OnNegativeResponse(PlayerStats playerStats) { 
        Debug.Log("Negative Response");
        playerStats.AddNegativeVote();
    }

}
#endif