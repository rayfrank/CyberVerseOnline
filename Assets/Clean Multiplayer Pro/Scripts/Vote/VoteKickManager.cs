#if CMPSETUP_COMPLETE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AvocadoShark;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VoteKickManager : NetworkBehaviour,IPlayerLeft
{
    private readonly Dictionary<PlayerRef, PlayerStats> _playerStatsMap = new Dictionary<PlayerRef, PlayerStats>();
    [SerializeField] private InputActionReference moveAction,lookAction,jumpAction;
    [SerializeField] private float voteTotalTimeInSecond = 10f;
    [SerializeField] private float popupShowTimeInSecond = 2f;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject bg,popupGameObject;
    [SerializeField] private GameObject votingPanel;
    [SerializeField] private VotePlayerUI votePlayerUI;
    [SerializeField] private Button startVoteKickBtn;
    [SerializeField] private TextMeshProUGUI remainingTime,popupText;
    private bool _isVotingStarted;
    private readonly Dictionary<PlayerRef,VotePlayerUI> _votePlayerUis = new Dictionary<PlayerRef,VotePlayerUI>();
    private readonly Dictionary<PlayerRef, List<PlayerRef>> _votes = new Dictionary<PlayerRef, List<PlayerRef>>();
    [Networked] private TickTimer VoteTime { get; set; }

    public void StartVoteKick()
    {
        if (!HasEnoughPlayers())
        {
            StartCoroutine(ShowPopUp("<color=#D96222>Not enough player to start vote kick</color>"));
            return;
        }
        Init_Rpc();
    }

    private void Update()
    {
        if(!_isVotingStarted)
            return;
        remainingTime.text = Mathf.RoundToInt(VoteTime.RemainingTime(Runner).GetValueOrDefault()) + "s";
    }

    public override void FixedUpdateNetwork()
    {
        if (VoteTime.Expired(Runner) && _isVotingStarted)
        {
            _isVotingStarted = false;
            HandleVoteResultsRPC();
        }
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Init_Rpc()
    {
        foreach (var i in SessionPlayers.instance.activePlayers)
        {
            _playerStatsMap.Add(i.Object.StateAuthority,i);
            if (i.Object.StateAuthority == Runner.LocalPlayer)
                continue;
            var obj = Instantiate(votePlayerUI, content);
            _votePlayerUis.Add(i.Object.StateAuthority,obj);
            obj.playerName.text =i.PlayerName.Value; //i.ToString();
            obj.voteButton.onClick.AddListener(() => Vote(i.Object.StateAuthority));
        }

        if (HasStateAuthority)
            VoteTime = TickTimer.CreateFromSeconds(Runner, voteTotalTimeInSecond);
        _isVotingStarted = true;
        votingPanel.SetActive(true);
        bg.SetActive(true);
        startVoteKickBtn.gameObject.SetActive(false);
        DisablePlayerInput();
    }

    private void Vote(PlayerRef playerRef)
    {
        foreach (var i in _votePlayerUis)
        {
            i.Value.voteButton.gameObject.SetActive(false);
        }

        RPCVote(playerRef,Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPCVote(PlayerRef voteReceiver,PlayerRef voteCaster)
    {
        if (!_votes.ContainsKey(voteReceiver))
        {
            _votes[voteReceiver] = new List<PlayerRef>();
            //_votes[Object.StateAuthority].Add(playerRef);
        }
        _votes[voteReceiver].Add(voteCaster);
        print($"receiver {voteReceiver} caster {voteCaster}");
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void HandleVoteResultsRPC()
    {
        if (!GetPlayerWithMaxVotes(out var playerToKick))
        {
            // Execute kick action on the player
            KickPlayer_Rpc(playerToKick);
        }
        else
        {
            HidePanel();
            StartCoroutine(ShowPopUp("<color=#D96222>No one got kicked out</color>"));
        }
    }

    private bool GetPlayerWithMaxVotes(out PlayerRef playerRef)
    {
        var maxVotes = 0;
        var isequal = false;
        playerRef = default;
        foreach (var kvp in _votes)
        {
            print($"{kvp.Key} {kvp.Value.Count}");
            if (kvp.Value.Count > maxVotes)
            {
                maxVotes = kvp.Value.Count;
                playerRef = kvp.Key;
                isequal = false;
            }
            else if (kvp.Value.Count == maxVotes)
            {
                isequal = true;
            }
        }

        return isequal;
    }

    //[Rpc(RpcSources.All, RpcTargets.All)]
    private void KickPlayer_Rpc(PlayerRef playerRef)
    {
        var playerStat = _playerStatsMap[playerRef];
        HidePanel();
        Action callback = Runner.LocalPlayer == playerRef
            ? () =>
            {
                Runner.Shutdown();
                var fusionManager = FindFirstObjectByType<FusionConnection>();
                if (fusionManager != null)
                {
                    Destroy(fusionManager);
                }

                SceneManager.LoadScene("Menu");
            }
            : null;
        StartCoroutine(ShowPopUp($"<color=#D96222>{playerStat.PlayerName} got kicked out</color>", callback));
    }

    private void HidePanel()
    {
        votingPanel.gameObject.SetActive(false);
        bg.SetActive(false);
        startVoteKickBtn.gameObject.SetActive(true);
        EnablePlayerInput();
        foreach (var i in new Dictionary<PlayerRef,VotePlayerUI>(_votePlayerUis))
        {
            _votePlayerUis.Remove(i.Key);
            Destroy(i.Value.gameObject);
        }

        _votes.Clear();
        _playerStatsMap.Clear();
        _votePlayerUis.Clear();
    }

    private IEnumerator ShowPopUp(string text,Action callback=null)
    {
        startVoteKickBtn.gameObject.SetActive(false);
        votingPanel.SetActive(false);
        popupText.text = text;
        bg.SetActive(true);
        popupGameObject.SetActive(true);
        DisablePlayerInput();
        yield return new WaitForSeconds(popupShowTimeInSecond);
        startVoteKickBtn.gameObject.SetActive(true);
        popupGameObject.SetActive(false);
        bg.SetActive(false);
        callback?.Invoke();
        EnablePlayerInput();
    }

    private bool HasEnoughPlayers()
    {
        return Runner.ActivePlayers.Count() > 2;
    }

    public void PlayerLeft(PlayerRef player)
    {
        PlayerLeftRPC(player);
    }

    [Rpc(RpcSources.All,RpcTargets.All)]
    private void PlayerLeftRPC(PlayerRef player)
    {
        _votes.Remove(player);
        foreach (var i in _votes)
        {
            i.Value.Remove(player);
        }
        _playerStatsMap.Remove(player);
    }

    private void DisablePlayerInput()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
        jumpAction.action.Disable();
    }
    private void EnablePlayerInput()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
        jumpAction.action.Enable();
    }
}
#endif