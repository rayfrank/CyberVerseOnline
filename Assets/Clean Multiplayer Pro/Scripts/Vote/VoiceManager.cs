#if CMPSETUP_COMPLETE
using Fusion;
using UnityEngine;
using Photon.Voice.Unity;
using StarterAssets;

public class VoiceManager : MonoBehaviour
{
    // Start is called before the first frame update
    public NetworkRunner runner;
    public Recorder recorder;
    private StarterAssetsInputs _input;
    private PlayerWorldUIManager _playerUIManager;


    // VOICE INPUT  
    private void Update()
    {
        if (!runner.IsConnectedToServer || ReferenceEquals(_input,null))
            return;

        recorder.TransmitEnabled = _input.pushToTalk;
        _playerUIManager.SetIsSpeaking(_input.pushToTalk);
    }

    public void Init(StarterAssetsInputs input, PlayerWorldUIManager pManager)
    {
        _input = input;
        _playerUIManager = pManager;
    }
}
#endif