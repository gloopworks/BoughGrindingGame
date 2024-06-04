using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem;

using static UnityEngine.Splines.SplineUtility;

using MixJam12.Gameplay.Rails;
using MixJam12.Enumerations;
using MixJam12.Utilities;

namespace MixJam12.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerRailGrind : PlayerController
    {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerJump playerJump;

        [Header("Grinding")]
        [SerializeField] private LayerMask railLayer;

        [Space]

        [SerializeField, Range(0.0f, 100.0f)] private float downwardGrindSpeed = 20.0f;
        [SerializeField, Range(0.0f, 100.0f)] private float upwardGrindSpeed = 20.0f;

        [Space]

        [SerializeField] private float grindAcceleration = 10f;

        [Space]

        [SerializeField, Range(0.0f, 1.0f)] private float verticalOffset;
        [SerializeField, Range(0.5f, 5.0f)] private float exitDistanceThreshold;

        private Rigidbody body;
        private Spline currentRail;
        private GrindState grindState;

        private float grindDirection;

        private float currentGrindSpeed;
        private float targetGrindSpeed;

        public override void Start()
        {
            base.Start();

            RailManager.Instance.OnRailTriggerEnteredEvent += OnRailEntered;

            body = GetComponent<Rigidbody>();
        }

        public override void SubscribeToInputActions()
        {
            playerInput.actions["Player/Jump"].performed += OnJump;
        }
        public override void UnsubscribeFromInputActions()
        {
            playerInput.actions["Player/Jump"].performed -= OnJump;
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (grindState == GrindState.Grinding)
            {
                EndRailGrind();
                playerJump.Jump();
            }
        }

        private void OnRailEntered(object sender, RailManager.RailTriggerEventArgs args)
        {
            if (grindState == GrindState.Inactive)
            {
                StartRailGrind(args.Container.Spline);
            }
        }

        private void StartRailGrind(Spline spline)
        {
            Debug.Log("Start Grind");

            playerMovement.DisableInputs();
            playerJump.DisableInputs();

            float t = SnapToRail(spline, out _);
            CalculateGrindDir(spline, t);

            grindState = GrindState.Grinding;

            body.excludeLayers = railLayer;

            currentRail = spline;
        }

        private void EndRailGrind()
        {
            Debug.Log("Exit Grind");

            playerMovement.EnableInputs();
            playerJump.EnableInputs();

            body.excludeLayers = 0;

            grindState = GrindState.Exiting;
        }

        private float SnapToRail(Spline spline, out float distance)
        {
            distance = GetNearestPoint(spline, body.position, out var nearestPoint, out float t);

            Vector3 up = spline.EvaluateUpVector(t);
            body.position = (Vector3)nearestPoint + (up * verticalOffset);

            return t;
        }

        private void CalculateGrindDir(Spline spline, float t)
        {
            Vector3 tangent = spline.EvaluateTangent(t);

            float dot = Vector3.Dot(body.velocity, tangent);
            grindDirection = dot > 0 ? 1 : -1;

            currentGrindSpeed = body.velocity.magnitude;
        }

        private void FixedUpdate()
        {
            if (grindState == GrindState.Grinding)
            {
                ProcessGrindMovement();
                return;
            }

            if (grindState == GrindState.Exiting)
            {
                float distance = GetNearestPoint(currentRail, body.position, out _, out _);

                if (distance >= exitDistanceThreshold)
                {
                    grindState = GrindState.Inactive;
                }
            }
        }

        private void ProcessGrindMovement()
        {
            float t = SnapToRail(currentRail, out float distance);

            if ((t == 1 || t == 0) && distance >= exitDistanceThreshold)
            {
                EndRailGrind();
                return;
            }

            Vector3 tangent = currentRail.EvaluateTangent(t);
            Vector3 velocity = tangent.normalized * grindDirection;

            targetGrindSpeed = Mathf.Lerp(downwardGrindSpeed, upwardGrindSpeed, velocity.y.Remap01(-1.0f, 1.0f));
            currentGrindSpeed = Mathf.MoveTowards(currentGrindSpeed, targetGrindSpeed, grindAcceleration * Time.fixedDeltaTime);

            body.velocity = velocity * currentGrindSpeed;
        }
    }
}