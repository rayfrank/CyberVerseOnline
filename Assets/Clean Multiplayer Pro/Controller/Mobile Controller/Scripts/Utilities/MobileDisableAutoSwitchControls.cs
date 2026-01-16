#if CMPSETUP_COMPLETE
using UnityEngine;
using UnityEngine.InputSystem;

namespace AvocadoShark
{
    public class MobileDisableAutoSwitchControls : MonoBehaviour
    {
        [Header("Target")]
        public PlayerInput playerInput;

        void Start()
        {
            DisableAutoSwitchControls();
        }

        void DisableAutoSwitchControls()
        {
            playerInput.neverAutoSwitchControlSchemes = true;
        }
    }
}
#endif
