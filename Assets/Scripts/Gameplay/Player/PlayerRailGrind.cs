using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem;

using static UnityEngine.Splines.SplineUtility;
using static MixJam12.Utilities.GeneralUtils;

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

        [Header("Grind Speed Values")]
        [SerializeField, Range(0.0f, 100.0f)] private float downwardGrindSpeed = 20.0f;
        [SerializeField, Range(0.0f, 100.0f)] private float upwardGrindSpeed = 20.0f;

        [Space]

        [SerializeField] private float grindAcceleration = 10f;

        [Header("Grind Settings")]
        [SerializeField] private LayerMask railLayer;

        [Space]

        [SerializeField, Range(0.0f, 10.0f)] private float verticalOffset;
        [SerializeField, Range(0.0f, 5.0f)] private float snapDistanceThreshold;

        [Space]

        [SerializeField, Range(0.5f, 5.0f)] private float exitDistanceThreshold;

        private Rigidbody body;

        private Transform currentRailTransform;
        private Spline currentRail;
        private GrindState grindState;

        private float grindDirection;

        private float currentGrindSpeed;
        private float targetGrindSpeed;

        private Vector3 railExitPosition;

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
                EndRailGrind(body.position);
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

            body.position = GetTargetRailPosition(parent, spline, out float t);
            CalculateGrindDir(spline, t);

            grindState = GrindState.Grinding;

            body.excludeLayers = railLayer;

            currentRail = spline;
            currentRailTransform = parent;
        }

        private void EndRailGrind(Vector3 railPosition)
        {
            Debug.Log("Exit Grind");

            playerMovement.EnableInputs();
            playerJump.EnableInputs();

            body.excludeLayers = 0;

            railExitPosition = railPosition;
            grindState = GrindState.Exiting;
        }

        private Vector3 GetTargetRailPosition(Transform parent, Spline spline, out float t)
        {
            Vector3 localPoint = parent.InverseTransformPoint(body.position);
            _ = GetNearestPoint(spline, localPoint, out var nearestPoint, out t);

            Vector3 up = spline.EvaluateUpVector(t);

            Vector3 global = parent.TransformPoint(nearestPoint);
            return global + (up * verticalOffset);
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
            float sqrThreshold = Mathf.Pow(exitDistanceThreshold, 2);

            if (SqrDistance(railExitPosition, body.position) > sqrThreshold)
            {
                grindState = GrindState.Inactive;
            }
        }

        private void ProcessGrindMovement()
        {
            Vector3 targetPosition = GetTargetRailPosition(currentRailTransform, currentRail, out float railT);
            Vector3 towardsTarget = (targetPosition - body.position).normalized;
            float distanceT = SqrDistance(targetPosition, body.position) / Mathf.Pow(snapDistanceThreshold, 2);

            if ((GetRemainingLength(railT) < 0.2f) && !currentRail.Closed)
            {
                EndRailGrind(body.position);
                return;
            }

            Vector3 tangent = currentRail.EvaluateTangent(railT);
            Vector3 velocity = tangent.normalized * grindDirection;

            targetGrindSpeed = Mathf.Lerp(downwardGrindSpeed, upwardGrindSpeed, velocity.y.Remap01(-1.0f, 1.0f));
            currentGrindSpeed = Mathf.MoveTowards(currentGrindSpeed, targetGrindSpeed, grindAcceleration * Time.fixedDeltaTime);

            body.velocity = Vector3.Lerp(velocity, towardsTarget, distanceT) * currentGrindSpeed;
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