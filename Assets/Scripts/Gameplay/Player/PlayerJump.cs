using UnityEngine;
using UnityEngine.InputSystem;

namespace MixJam12.Gameplay.Player
{
    public class PlayerJump : PlayerController
    {
        public class OnJumpEventArgs : System.EventArgs
        {
            public float JumpForce { get; private set; }
            public OnJumpEventArgs(float jumpForce)
            {
                JumpForce = jumpForce;
            }
        }

        public event System.EventHandler<OnJumpEventArgs> OnJumpEvent;
        public bool InputsEnabled { get; private set; }

        [Header("Jump Values")]
        [SerializeField, Range(0f, 30f)] private float jumpForce = 16f;
        [SerializeField, Range(0f, 1f)] private float jumpBuffer = 0.1f;

        private Rigidbody body;
        private CollisionCheck collisionCheck;

        private float jumpBufferCounter;
        private bool jumpingThisFrame;
        private float BodyYVelocity
        {
            set
            {
                body.velocity = new Vector3(body.velocity.x, value, body.velocity.z);
            }
        }

        public override void Start()
        {
            base.Start();

            body = GetComponent<Rigidbody>();

            collisionCheck = GetComponent<CollisionCheck>();
        }

        public override void SubscribeToInputActions()
        {
            playerInput.actions["Player/Jump"].performed += ReceiveJumpInput;
        }

        public override void UnsubscribeFromInputActions()
        {
            playerInput.actions["Player/Jump"].performed -= ReceiveJumpInput;
        }

        public void DisableInputs()
        {
            UnsubscribeFromInputActions();
            jumpBufferCounter = 0f;

            InputsEnabled = false;
        }

        public void EnableInputs()
        {
            SubscribeToInputActions();

            InputsEnabled = true;
        }

        private void FixedUpdate()
        {
            if (jumpBufferCounter > 0f && collisionCheck.OnSurface && !jumpingThisFrame)
            {
                Jump();
                return;
            }
            jumpBufferCounter -= Time.fixedDeltaTime;
            jumpingThisFrame = false;
        }

        private void ReceiveJumpInput(InputAction.CallbackContext ctx)
        {
            if (collisionCheck.OnSurface)
            {
                Jump();
                return;
            }
            jumpBufferCounter = jumpBuffer;
        }

        public void Jump()
        {
            BodyYVelocity = jumpForce;
            jumpBufferCounter = 0f;
            jumpingThisFrame = true;

            OnJumpEvent?.Invoke(this, new(jumpForce));
        }
    }
}