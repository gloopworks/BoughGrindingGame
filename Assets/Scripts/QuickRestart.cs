using UnityEngine;
using UnityEngine.InputSystem;

namespace MixJam12.Gameplay
{
    public class QuickRestart : PlayerController
    {
        [SerializeField] private Rigidbody playerBody;

        public override void SubscribeToInputActions()
        {
            playerInput.actions["Player/Restart"].performed += OnRestartInput;
        }

        private void OnRestartInput(InputAction.CallbackContext ctx)
        {
            playerBody.velocity = Vector3.zero;
            playerBody.position = Vector3.zero;
        }

        public override void UnsubscribeFromInputActions()
        {
            
        }
    }
}