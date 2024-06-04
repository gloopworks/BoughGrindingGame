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

        [SerializeField, Range(0.0f, 10.0f)] private float verticalOffset;
        [SerializeField, Range(0.5f, 5.0f)] private float exitDistanceThreshold;

        private Rigidbody body;

        private Transform currentRailTransform;
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
                StartRailGrind(args.Container.transform, args.Container.Spline);
            }
        }

        private void StartRailGrind(Transform parent, Spline spline)
        {
            Debug.Log("Start Grind");

            playerMovement.DisableInputs();
            playerJump.DisableInputs();

            float t = SnapToRail(parent, spline);
            CalculateGrindDir(spline, t);

            grindState = GrindState.Grinding;

            body.excludeLayers = railLayer;

            currentRail = spline;
            currentRailTransform = parent;
        }

        private void EndRailGrind()
        {
            Debug.Log("Exit Grind");

            playerMovement.EnableInputs();
            playerJump.EnableInputs();

            body.excludeLayers = 0;

            grindState = GrindState.Exiting;
        }

        private float SnapToRail(Transform parent, Spline spline)
        {
            Vector3 localPoint = parent.InverseTransformPoint(body.position);
            _ = GetNearestPoint(spline, localPoint, out var nearestPoint, out float t);

            Vector3 up = spline.EvaluateUpVector(t);

            Vector3 global = parent.TransformPoint(nearestPoint);
            body.position = global + (up * verticalOffset);

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
                TryExitGrind();
            }
        }

        private void TryExitGrind()
        {
            Vector3 localPoint = currentRailTransform.InverseTransformPoint(body.position);
            float distance = GetNearestPoint(currentRail, localPoint, out _, out _);

            if (distance >= exitDistanceThreshold)
            {
                grindState = GrindState.Inactive;
            }
        }

        private void ProcessGrindMovement()
        {
            float t = SnapToRail(currentRailTransform, currentRail);

            if ((GetRemainingLength(t) < 0.2f) && !currentRail.Closed)
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

        private float GetRemainingLength(float t)
        {
            if (grindDirection > 0)
            {
                return currentRail.GetLength() * (1 - t);
            }

            return currentRail.GetLength() * t;
        }
    }
}