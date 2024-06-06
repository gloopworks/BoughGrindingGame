using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

using static UnityEngine.Splines.SplineUtility;
using static MixJam12.Utilities.GeneralUtils;

using MixJam12.Gameplay.Rails;

namespace MixJam12.Gameplay.Player.Spells
{
    public class RailGun : PlayerController
    {
        [Header("References")]
        [SerializeField] private Transform lookTransform;
        [SerializeField] private LayerMask groundLayers;

        [Space]

        [SerializeField] private GameObject railPrefab;

        private Transform splineTransform;

        private Spline currentSpline;
        private SplineExtrude currentExtrude;
        private SplineInstantiate currentInstantiate;

        [Header("Placement")]
        [SerializeField] private LayerMask layersBlockingPlacement;

        [Space]

        [SerializeField, Range(0.0f, 10.0f)] private float floorOffset = 0.5f;
        [SerializeField, Range(0.0f, 10.0f)] private float ceilingOffset = 3f;

        [Space]

        [SerializeField, Range(0.0f, 50.0f)] private float maxTangentDistance = 10f;
        [SerializeField, Range(0.0f, 50.0f)] private float minTangentDistance = 1f;
        [SerializeField] private AnimationCurve tangentDistanceCurve;

        [Space]

        [SerializeField, Range(0.0f, 10.0f)] private float initialRailLength = 5f;

        private bool createNewContainer = true;

        public override void Start()
        {
            base.Start();

            SplineContainer prefabSplineContainer = railPrefab.GetComponent<SplineContainer>();
            if (prefabSplineContainer.Spline.Count < 2) { Debug.Log("Spline Prefab is missing knots."); }

            _ = railPrefab.GetComponent<SplineExtrude>();
            _ = railPrefab.GetComponent<SplineInstantiate>();
        }

        public override void SubscribeToInputActions()
        {
            playerInput.actions["Player/Fire"].performed += OnFire;
        }

        public override void UnsubscribeFromInputActions()
        {
            playerInput.actions["Player/Fire"].performed -= OnFire;
        }

        private void OnFire(InputAction.CallbackContext ctx)
        {
            Fire();
        }

        private void Fire()
        {
            Ray ray = new(lookTransform.position, lookTransform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayers, QueryTriggerInteraction.Ignore))
            {
                if (createNewContainer)
                {
                    InstantiateSplineContainer(ray.direction, hit.point, hit.normal);
                    createNewContainer = false;
                    return;
                }

                TryAddKnotToSpline(ray.direction, hit.point, hit.normal);
            }
        }
        private bool TryAddKnotToSpline(Vector3 direction, Vector3 point, Vector3 normal)
        {
            Vector3 worldPlacement = point + (normal * GetSurfaceOffset(normal.y));
            Vector3 worldPrevious = splineTransform.TransformPoint(currentSpline[^1].Position);
            Vector3 forward = worldPlacement - worldPrevious;
            forward = Vector3.ProjectOnPlane(forward, normal).ExcludeYAxis();
            forward = forward.normalized;

            Vector3 localPlacement = splineTransform.InverseTransformPoint(worldPlacement);
            Vector3 previousTangent = (Quaternion)currentSpline[^1].Rotation * (Vector3)currentSpline[^1].TangentOut;

            Vector3 rayOrigin = worldPrevious + previousTangent;
            float maxDistance = (worldPlacement - rayOrigin).magnitude;

            Ray ray = new(rayOrigin, worldPlacement - rayOrigin);
            Debug.DrawRay(rayOrigin, worldPlacement - rayOrigin, Color.magenta, 5f);
            if (Physics.Raycast(ray, maxDistance, layersBlockingPlacement, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Debug.DrawRay(point, normal * GetSurfaceOffset(normal.y), Color.green, 5f);
            AddKnotToSpline(forward, previousTangent, localPlacement);

            return true;
        }

        private void AddKnotToSpline(Vector3 forward, Vector3 previousTangent, Vector3 localPlacement)
        {
            float adjustedTangentLength = GetAdjustedTangentLength(forward, previousTangent);

            currentSpline.Add(new(localPlacement, -adjustedTangentLength * forward, adjustedTangentLength * forward), TangentMode.Continuous);

            Vector3 worldPlacement = splineTransform.TransformPoint(localPlacement);
            Vector3 worldPrevious = splineTransform.TransformPoint(currentSpline[^1].Position);
            Debug.DrawRay(worldPlacement, adjustedTangentLength * forward, Color.red, 10f);
            Debug.DrawRay(worldPrevious, previousTangent, Color.yellow, 10f);

            currentExtrude.Rebuild();
            currentInstantiate.UpdateInstances();
        }

        private void InstantiateSplineContainer(Vector3 direction, Vector3 point, Vector3 normal)
        {
            Vector3 forward = Vector3.ProjectOnPlane(direction, normal).ExcludeYAxis().normalized;
            Vector3 placement = point + (normal * GetSurfaceOffset(normal.y));

            GameObject clone = Instantiate(railPrefab, placement, Quaternion.identity);

            splineTransform = clone.transform;

            SplineContainer container = clone.GetComponent<SplineContainer>();
            currentExtrude = clone.GetComponent<SplineExtrude>();
            currentInstantiate = clone.GetComponent<SplineInstantiate>();

            currentSpline = container.Spline;

            currentSpline[0] = new(-forward * (initialRailLength / 2f), -forward, forward);
            currentSpline[1] = new(forward * (initialRailLength / 2f), -forward, forward);

            clone.GetComponent<MeshFilter>().mesh = new Mesh();

            currentExtrude.Rebuild();
            currentInstantiate.UpdateInstances();

            RailManager.Instance.UpdateSplineInstantiators();
        }

        private float GetSurfaceOffset(float yNormal)
        {
            float t = yNormal.Remap01(-1, 1);

            return Mathf.Lerp(ceilingOffset, floorOffset, t);
        }

        private float GetAdjustedTangentLength(Vector3 forward, Vector3 previousTangent)
        {
            // Use the minimum tangent distance when both tangents are similar
            float dot = Vector3.Dot(forward, previousTangent.normalized);
            float t = tangentDistanceCurve.Evaluate(dot.Remap01(-1, 1));

            return Mathf.Lerp(maxTangentDistance, minTangentDistance, t);
        }
    }
}