#if CMPSETUP_COMPLETE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AvocadoShark;
using Fusion;
using System;

public class SessionPlayers : MonoBehaviour {

    public static SessionPlayers instance = null;

    //public Dictionary<int, PlayerStats> activePlayers = new Dictionary<int, PlayerStats>();             // player Id has a playerStats

    public List<PlayerStats> activePlayers = new List<PlayerStats>();             // player Id has a playerStats

    public Action<PlayerStats> OnPlayerAddedToSession;
    public Action<PlayerStats> OnPlayerRemovedFromSession;
    public Action<PlayerStats> OnVoteKickInitiated;
    public Action<PlayerStats> OnVoteKickFinished;

    public void AddPlayer(PlayerRef playerRef) {
        //activePlayers.Add(playerRef, null);
    }

    public void AddPlayerStateToPlayerRef(PlayerStats playerStats) {
        // activePlayers.
    }

    public void AddPlayer(PlayerStats playerStats) {
        Debug.Log(playerStats.PlayerName);
        activePlayers.Add(playerStats);
        OnPlayerAddedToSession?.Invoke(playerStats);
    }

    public void RemovePlayer(PlayerStats playerStats) {
        activePlayers.Remove(playerStats);
        OnPlayerRemovedFromSession?.Invoke(playerStats);
    }

    public void AddVoteKick(PlayerStats playerStats) {
        OnVoteKickInitiated?.Invoke(playerStats);
    }

    public void RemoveVoteKick(PlayerStats playerStats) {
        OnVoteKickFinished?.Invoke(playerStats);
    }

    private void Awake() {
        instance = this;
    }
}
#endif