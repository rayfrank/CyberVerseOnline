#if CMPSETUP_COMPLETE
using System;
using UnityEditor;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[SerializeField] private bool isPlayerWritingChat = false;

		public InputActionReference PushToTalkAction,moveAction,lookAction,jumpAction;
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Voice Input")]
		public bool pushToTalk;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
		public void OnCursorState(InputValue value)
		{
			if (Cursor.lockState is CursorLockMode.None)
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
			else
			{
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
		}

		private void EnablePushToTalk(InputAction.CallbackContext context) {
            if (isPlayerWritingChat)
                return;
            pushToTalk = true;
		}

		private void DisablePushToTalk(InputAction.CallbackContext context) {
            if (isPlayerWritingChat)
                return;
            pushToTalk = false;
        }
#endif


		public void MoveInput(Vector2 newMoveDirection) {
			if (isPlayerWritingChat)
				return;
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection) {
            if (isPlayerWritingChat)
                return;
            look = newLookDirection;
		}

		public void JumpInput(bool newJumpState) {
            if (isPlayerWritingChat)
                return;
            jump = newJumpState;
		}

		public void SprintInput(bool newSprintState) {
            if (isPlayerWritingChat)
                return;
            sprint = newSprintState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}

		public void DisablePlayerInput()
		{
			isPlayerWritingChat = true;
			move = Vector2.zero;
			look = Vector2.zero;
		}
		public void EnablePlayerInput()
		{
			isPlayerWritingChat = false;
		}

		private void Awake() {
			PushToTalkAction.action.Enable();
			PushToTalkAction.action.performed += EnablePushToTalk;
            PushToTalkAction.action.canceled += DisablePushToTalk;
        }
	}
	
}
#endif