#if CMPSETUP_COMPLETE
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInputRebinding : MonoBehaviour
{
    private enum MovementKeys
    {
        W = 1,
        S = 2,
        A = 3,
        D = 4
    }

    [Header("References")]
    [SerializeField] private InputActionReference inputActionReference;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI text;

    [Header("Movement Keys Bindings")] [SerializeField]
    private bool isCompositeAction;

    [SerializeField] private MovementKeys movementKey;

    private SettingUIHandler _settingUIHandler;

    private void Awake()
    {
        button.onClick.AddListener(StartRebinding);
    }

    public void Initialize(SettingUIHandler settingUIHandler)
    {
        _settingUIHandler = settingUIHandler;
        text.text = GetKeyName();
    }

    private void StartRebinding()
    {
        inputActionReference.action.Disable();
        var bindingIndex = isCompositeAction ? (int)movementKey : -1;
        inputActionReference.action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(RebindComplete)
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(RebindComplete)
            .Start();
        text.text = "Rebinding...";
    }

    private void RebindComplete(InputActionRebindingExtensions.RebindingOperation operation)
    {
        inputActionReference.action.Enable();
        text.text = GetKeyName();
        operation.Dispose();
        _settingUIHandler.SaveBindings();
    }
    private string GetKeyName()
    {
        var bindingIndex = isCompositeAction
            ? (int)movementKey
            : inputActionReference.action.GetBindingIndexForControl(inputActionReference.action.controls[0]);
        return InputControlPath.ToHumanReadableString(
            inputActionReference.action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }
}
#endif