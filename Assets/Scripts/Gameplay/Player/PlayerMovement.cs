using UnityEngine;
using UnityEngine.InputSystem;

using static MixJam12.Utilities.GeneralUtils;
using static MixJam12.Gameplay.Player.CollisionCheck;

namespace MixJam12.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(CollisionCheck), typeof(BoxCollider))]
    public class PlayerMovement : PlayerController
    {
        #region Parameters
        #region Events
        public class OnCrouchEventArgs : System.EventArgs
        {
            public bool IsCrouched { get; private set; }
            public float HeightDifference { get; private set; }

            public OnCrouchEventArgs(bool isCrouched, float heightDifference)
            {
                IsCrouched = isCrouched;
                HeightDifference = heightDifference;
            }
        }

        public event System.EventHandler<OnCrouchEventArgs> OnCrouchEvent;
        #endregion

        #region Serialized
        [Header("References")]
        [SerializeField] private Transform lookTransform;
        [SerializeField] private LayerMask groundLayers;

        [Header("Speed Values")]
        [SerializeField] private float maxSpeed = 12f;
        [SerializeField] private float crouchSpeed = 6f;

        [Space, SerializeField] private float groundAcceleration = 120f;
        [SerializeField] private float airAcceleration = 24f;

        [Space, SerializeField, Range(0f, 30f)] private float groundFriction = 8f;
        [SerializeField, Range(0f, 30f)] private float slideFriction = 1f;
        [SerializeField, Range(0f, 30f)] private float airFriction = 1f;

        [Header("Crouch/Slide Values")]
        [SerializeField, Range(0f, 5f)] private float defaultPlayerHeight = 3f;
        [SerializeField, Range(0f, 5f)] private float crouchedHeight = 1f;

        [Space, SerializeField, Range(0f, 10f)] private float minSlideDuration = 0.5f;
        [SerializeField, Range(0f, 30f)] private float groundSlideForce = 8f;
        [SerializeField, Range(0f, 30f)] private float landingSlideForce = 4f;
        private float heightDifference => defaultPlayerHeight - crouchedHeight;
        #endregion

        #region Exposed
        public bool IsCrouched { get; private set; }
        public bool IsSliding => IsCrouched && slideCounter > 0f && collisionCheck.OnSurface;
        public bool InputsEnabled { get; private set; }
        public Vector3 WishDir { get; private set; }
        #endregion

        #region Private
        private Rigidbody body;
        private CollisionCheck collisionCheck;
        private BoxCollider boxCollider;

        private Vector3 moveInput;

        private float addSpeed;
        private float currentSpeed;
        private float acceleration;

        private bool crouchInput;
        private float slideCounter;
        #endregion

        #endregion
        public override void Start()
        {
            base.Start();

            body = GetComponent<Rigidbody>();
            
            collisionCheck = GetComponent<CollisionCheck>();
            boxCollider = GetComponent<BoxCollider>();

            collisionCheck.OnHitGroundEvent += OnHitGround;
        }

        public override void SubscribeToInputActions()
        {
            playerInput.actions["Player/Move"].performed += ReceiveMoveInput;
            playerInput.actions["Player/Move"].canceled += ReceiveMoveInput;

            playerInput.actions["Player/Crouch"].performed += ReceiveCrouchInput;
            playerInput.actions["Player/Crouch"].canceled += ReceiveCrouchInput;
        }

        public override void UnsubscribeFromInputActions()
        {
            playerInput.actions["Player/Move"].performed -= ReceiveMoveInput;
            playerInput.actions["Player/Move"].canceled -= ReceiveMoveInput;

            playerInput.actions["Player/Crouch"].performed -= ReceiveCrouchInput;
            playerInput.actions["Player/Crouch"].canceled -= ReceiveCrouchInput;
        }

        private void FixedUpdate()
        {
            ApplyFriction();
            if (!IsSliding)
            {
                ProcessHorizontalMovement();
            }
            ProcessCrouch();

            if (collisionCheck.OnGround)
            {
                slideCounter -= Time.fixedDeltaTime;
            }
        }

        public void DisableInputs()
        {
            UnsubscribeFromInputActions();
            moveInput = Vector3.zero;
            crouchInput = false;

            InputsEnabled = false;
        }

        public void EnableInputs()
        {
            SubscribeToInputActions();

            InputsEnabled = true;
        }

        #region Input Methods
        private void ReceiveMoveInput(InputAction.CallbackContext ctx)
        {
            Vector2 input = ctx.ReadValue<Vector2>();
            moveInput = new Vector3(input.x, 0f, input.y);
        }

        private void ReceiveCrouchInput(InputAction.CallbackContext ctx)
        {
            crouchInput = ctx.performed;
        }
        #endregion

        #region Movement Methods

        private void OnHitGround(object sender, OnHitGroundEventArgs args)
        {
            if (IsCrouched && body.velocity.ExcludeYAxis().sqrMagnitude > 0.1f)
            {
                EnterSlide(true);
            }
        }

        #region Crouch
        private void ProcessCrouch()
        {
            if (!IsCrouched && crouchInput)
            {
                EnterCrouch(collisionCheck.OnSurface);
            }

            if (IsCrouched && !crouchInput && CanExitCrouch())
            {
                ExitCrouch();
            }
        }

        public void EnterCrouch(bool onSurface)
        {
            Debug.Log("Crouch");

            IsCrouched = true;

            boxCollider.size = new Vector3(boxCollider.size.x, crouchedHeight, boxCollider.size.z);
            boxCollider.center -= heightDifference / 2 * Vector3.up;

            if (body.velocity.ExcludeYAxis().sqrMagnitude > 0.1f && onSurface)
            {
                EnterSlide(false);
            }

            OnCrouchEvent?.Invoke(this, new(IsCrouched, heightDifference));
        }

        private void ExitCrouch()
        {
            IsCrouched = false;

            boxCollider.size = new Vector3(boxCollider.size.x, defaultPlayerHeight, boxCollider.size.z);
            boxCollider.center += heightDifference / 2 * Vector3.up;

            OnCrouchEvent?.Invoke(this, new(IsCrouched, heightDifference));
        }

        private bool CanExitCrouch()
        {
            return !Physics.Raycast(transform.position + boxCollider.center, Vector3.up, heightDifference, groundLayers);
        }
        #endregion

        #region Slide
        public void EnterSlide(bool landingSlide)
        {
            Debug.Log("Slide");

            slideCounter = CalculateSlideDuration(out float proportion);
            Vector3 direction = collisionCheck.OnGround ? body.velocity.ExcludeYAxis().normalized : body.velocity.normalized;
            float slideForce = landingSlide ? landingSlideForce : groundSlideForce;

            body.velocity += direction * (slideForce * Mathf.Clamp01(proportion));
        }

        private float CalculateSlideDuration(out float proportion)
        {
            float velocity = body.velocity.magnitude;
            float multiplier = 1 / maxSpeed;

            proportion = velocity * multiplier;

            return minSlideDuration * proportion;
        }
        #endregion

        #region Horizontal Movement
        private void ProcessHorizontalMovement()
        {
            WishDir = CalculateWishDir();

            float speed = IsCrouched && collisionCheck.OnSurface ? crouchSpeed : maxSpeed;
            acceleration = collisionCheck.OnSurface ? groundAcceleration : airAcceleration;

            currentSpeed = Vector3.Dot(body.velocity, WishDir);
            addSpeed = Mathf.Clamp(speed - currentSpeed, 0f, acceleration * Time.fixedDeltaTime);

            body.velocity += WishDir * addSpeed;
            
            DrawMovementRays();
        }

        #region Calculation
        private Vector3 CalculateWishDir()
        {
            Vector3 wishDir = lookTransform.forward * moveInput.z;
            wishDir += lookTransform.right * moveInput.x;

            if (collisionCheck.OnSlope)
            {
                wishDir = Vector3.ProjectOnPlane(wishDir, collisionCheck.SlopeNormal);
                return wishDir.normalized;
            }
            wishDir.y = 0f;

            return wishDir.normalized;
        }

        private void ApplyFriction()
        {
            if (!collisionCheck.OnSurface)
            {
                body.AddForce(body.velocity * -airFriction);
                return;
            }
            if (IsSliding)
            {
                body.AddForce(body.velocity * -slideFriction);
                return;
            }

            body.AddForce(body.velocity * -groundFriction);
        }
        #endregion
        #endregion
        #endregion

        #region Debug Methods
        private void DrawMovementRays()
        {
            Debug.DrawRay(transform.position, lookTransform.forward, Color.red);
            Debug.DrawRay(transform.position, WishDir, Color.blue);
            Debug.DrawRay(transform.position, new Vector3(body.velocity.x, 0f, body.velocity.z), Color.green);
        }
        #endregion
    }
}