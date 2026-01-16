#if CMPSETUP_COMPLETE
using UnityEngine;
using Fusion;
using TMPro;
using System;
using UnityEngine.SceneManagement;
using System.Collections;

namespace AvocadoShark
{
    public class PlayerStats : NetworkBehaviour
    {
        private ChangeDetector _changeDetector;
        [Networked] public bool IsDisconnecting { get; set; }
        [Networked] public TickTimer VoteTime { get; set; }
        [Networked] public NetworkString<_32> PlayerName { get; set; }
        [Networked] public NetworkString<_32> VoteInitiatorPlayerName { get; set; }
        [Networked] public NetworkBool VoteKick { get; set; }
        [Networked] public int PositiveVotes { get; set; }
        [Networked] public int NegativeVotes { get; set; }

        public int maxVoteTime = 15;


        public bool isVoteInitiator = false;
        public Action<int> OnPositiveVotesChanged, OnNegativeVotesChanged, OnVoteTimeUpdated;
        public Action<bool> OnSpeaking;

        [SerializeField] TextMeshPro playerNameLabel;

        public static PlayerStats instance;

        public Action<string> OnPlayerStatsReady;
        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            GetComponent<PlayerWorldUIManager>().OnSpeaking += Speaking;
            if (HasStateAuthority)
            {
                PlayerName = FusionConnection.Instance._playerName;
                OnPlayerStatsReady?.Invoke(PlayerName.ToString());
                playerNameLabel.text = !HasStateAuthority ? PlayerName.ToString() : "";
                Debug.Log(PlayerName + " Has state authority");
                if (instance == null)
                {
                    instance = this;
                }
            }
            else
            {
                SessionPlayers.instance.AddPlayer(this);
                playerNameLabel.text = !HasStateAuthority ? PlayerName.ToString() : "";
            }

        }

        public override void Render()
        {
            foreach (var change in _changeDetector.DetectChanges(this, out var previousBuffer, out var currentBuffer))
            {
                switch (change)
                {
                    case nameof(PlayerName):
                        HandleChangeDetection<NetworkString<_32>>(nameof(PlayerName), previousBuffer, currentBuffer,
                            UpdatePlayerName);
                        break;
                    case nameof(VoteKick):
                        HandleChangeDetection<NetworkBool>(nameof(VoteKick), previousBuffer, currentBuffer,
                            OnVoteKickStateChanged);
                        break;
                    case nameof(PositiveVotes):
                        HandleChangeDetection<int>(nameof(PositiveVotes), previousBuffer, currentBuffer,
                            OnPositiveVote);
                        break;
                    case nameof(NegativeVotes):
                        HandleChangeDetection<int>(nameof(NegativeVotes), previousBuffer, currentBuffer,
                            OnNegativeVote);
                        break;
                }
            }
        }

        private void HandleChangeDetection<T>(string propertyName, NetworkBehaviourBuffer previousBuffer,
            NetworkBehaviourBuffer currentBuffer, Action<T, T> callback) where T : unmanaged
        {
            var reader = GetPropertyReader<T>(propertyName);
            var (previous, current) = reader.Read(previousBuffer, currentBuffer);
            callback(previous, current);
        }
        private void Update()
        {
            if(!VoteKick)
                return;
            OnVoteTimeUpdated?.Invoke(Mathf.RoundToInt(VoteTime.RemainingTime(Runner).GetValueOrDefault()));
        }

        public override void FixedUpdateNetwork()
        {
            if (VoteTime.Expired(Runner) && VoteKick)
            {
                VoteKick = false;
            }
        }
        
        // public override void FixedUpdateNetwork()
        // {
        //     if (Object.HasStateAuthority)
        //     {
        //         if (VoteTime.Expired(Runner) && VoteKick)
        //         {
        //             VoteKick = false;
        //         }
        //     }
        //
        //     OnVoteTimeUpdated?.Invoke(Mathf.FloorToInt((float)VoteTime.RemainingTime(Runner)));
        // }

        protected void UpdatePlayerName(NetworkString<_32> previous, NetworkString<_32> current)
        {
            SessionPlayers.instance.AddPlayer(this);
            playerNameLabel.text = !HasStateAuthority ? current.ToString() : "";
        }

        public void InitializeVoteKick()
        {
            Debug.Log("InitializeVoteKick");

            if (Object.HasStateAuthority)
            {
                PositiveVotes = PositiveVotes + 1;
            }

            if (NotEnoughPlayers())
            {
                // player count needs to be more than 2 for vote kick to work
                Debug.Log("Not enough players for vote kick");
                return;
            }

            if (IsDisconnecting)
            {
                Debug.Log("Player disconnection in process");
                return;
            }

            if (VoteKick == true)
            {
                Debug.Log($"Votekick already in process");
                return;
            }

            if (Runner.GameMode == GameMode.Shared)
            {
                Debug.Log($"Initializing Vote kick for {Runner.GameMode} Mode");
                if (Object.HasStateAuthority)
                {
                    VoteKick = true;
                    VoteInitiatorPlayerName = PlayerName;
                    VoteTime = TickTimer.CreateFromSeconds(Runner, maxVoteTime);
                }
                else
                {
                    RPC_BeginVoteKick();
                    isVoteInitiator = true;
                }
            }
            else
            {
                Debug.Log($"Initializing Vote kick for {Runner.GameMode} Mode");
            }
        }

        public void OnVoteKickStateChanged(NetworkBool previous, NetworkBool current)
        {
            Debug.Log("Vote kick state changed");
            Debug.Log($"positive votes {PositiveVotes}");
            if (current)
            {
                //if (!changed.Behaviour.HasStateAuthority) {
                SessionPlayers.instance.AddVoteKick(this);
                // if (HasStateAuthority)
                //     AddNegativeVote();
                // if (isVoteInitiator)
                // {
                //     isVoteInitiator = false;
                //     RPC_AddPositiveVote();
                // }
                PositiveVotes = 0;
                NegativeVotes = 0;
                //}
            }
            else 
            {
                if (HasStateAuthority)
                {
                    if (PositiveVotes > NegativeVotes)
                    {
                        IsDisconnecting = true;
                        RemovePlayer();
                        RPC_PlayerVoteResultMessage(
                            $"Vote kick for {PlayerName} has passed");
                    }
                    else
                    {
                        RPC_PlayerVoteResultMessage(
                            $"Vote kick for {PlayerName} has failed");
                    }

                    PositiveVotes = 0;
                    NegativeVotes = 0;
                }

                SessionPlayers.instance.RemoveVoteKick(this);
            }
        }

        public int GetNegativeVotes()
        {
            return (SessionPlayers.instance.activePlayers.Count - PositiveVotes - 1);
            // incase of 3 players 
            // total count = 3
            // criteria for votekick = 2 positive votes
        }

        public bool NotEnoughPlayers()
        {
            return (SessionPlayers.instance.activePlayers.Count <= 2);
        }

        public void AddPositiveVote()
        {
            if (!VoteKick)
                return;
            if (Object.HasStateAuthority)
                PositiveVotes += 1;
            else
                RPC_AddPositiveVote();
        }

        public void AddNegativeVote()
        {
            if (!VoteKick)
                return;
            if (Object.HasStateAuthority)
                NegativeVotes += 1;
            else
                RPC_AddNegativeVote();
        }

        public void RemovePlayer()
        {
            if (Object.HasStateAuthority)
            {
                Debug.Log("Shutting Down");
                StartCoroutine(RemovePlayerAfterDelay(3f));
            }
        }

        private IEnumerator RemovePlayerAfterDelay(float time)
        {
            yield return new WaitForSeconds(time);
            Runner.Shutdown();
            SceneManager.LoadScene(0);
        }

        public void OnPositiveVote(int previous, int current)
        {
            OnPositiveVotesChanged?.Invoke(current);
        }

        public void OnNegativeVote(int previous, int current)
        {
            OnNegativeVotesChanged?.Invoke(current);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            SessionPlayers.instance.RemovePlayer(this);
            if (VoteKick)
            {
                SessionPlayers.instance.RemoveVoteKick(this);
                RPC_PlayerVoteResultMessage(
                    $"Vote kick failed");
            }
        }

        [Rpc(sources: RpcSources.Proxies, targets: RpcTargets.StateAuthority)]
        public void RPC_BeginVoteKick()
        {
            Debug.Log("RPC_BeginVoteKick");
           // NegativeVotes = 1;                  // first negative vote for player who is being voted out
            VoteKick = true;
            VoteTime = TickTimer.CreateFromSeconds(Runner, maxVoteTime);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_PlayerVoteResultMessage(string message)
        {
            Debug.Log("RPC_PlayerVoteResultMessage");
            InfoPanel.instance.AddMessage(message);
        }

        [Rpc(sources: RpcSources.Proxies, targets: RpcTargets.StateAuthority)]
        public void RPC_AddPositiveVote()
        {
            PositiveVotes += 1;
        }

        [Rpc(sources: RpcSources.Proxies, targets: RpcTargets.StateAuthority)]
        public void RPC_AddNegativeVote()
        {
            Debug.Log("RPC_AddNegativeVote");
            NegativeVotes += 1;
        }

        private void Speaking(bool value)
        {
            OnSpeaking?.Invoke(value);
        }
    }
}
#endif