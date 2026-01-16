using Fusion;
using UnityEngine;

public class TerminalNetworkSync : NetworkBehaviour
{
    public PasswordLessonTerminal terminal;

    public override void FixedUpdateNetwork()
    {
        if (TerminalSharedState.Instance == null) return;

        // Update UI if text changed
        terminal.ForceSetScreen(TerminalSharedState.Instance.ScreenText ?? string.Empty);


        // Update current step
        terminal.ForceSetStep(TerminalSharedState.Instance.Step);
    }
}
