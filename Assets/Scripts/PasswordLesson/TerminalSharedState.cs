using Fusion;
using UnityEngine;

public class TerminalSharedState : NetworkBehaviour
{
    public static TerminalSharedState Instance;

    // Which step of the lesson we’re in (0,1,2,3…)
    [Networked] public int Step { get; set; }

    // Shared text that all players should see on the screen
    [Networked] public string ScreenText { get; set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        // Only the state authority sets initial values
        if (Object.HasStateAuthority)
        {
            Step = 0;
            ScreenText = "NURU CYBER LAB - Type 'ready' to begin.\n";
        }
    }

    // --------- called from local terminal when the text changes ---------

    public void UpdateScreen(string newText)
    {
        if (Object.HasStateAuthority)
        {
            ScreenText = newText;
        }
        else
        {
            RPC_UpdateScreen(newText);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_UpdateScreen(string newText)
    {
        ScreenText = newText;
    }

    public void UpdateStep(int newStep)
    {
        if (Object.HasStateAuthority)
        {
            Step = newStep;
        }
        else
        {
            RPC_UpdateStep(newStep);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_UpdateStep(int newStep)
    {
        Step = newStep;
    }
}
