using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

using static UnityEngine.Splines.SplineUtility;

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
        [SerializeField] private float offsetFromWall = 0.5f;
        [SerializeField] private float tangentDistance = 2f;

        [Space]

        [SerializeField] private float minDistanceFromSpline = 1.5f;

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

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayers))
            {
                Debug.Log(LayerMask.LayerToName(hit.transform.gameObject.layer));

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
            Vector3 placement = point + (normal * offsetFromWall);
            Vector3 forward = (placement - (Vector3)currentSpline[^1].Position).normalized;
            forward = Vector3.ProjectOnPlane(forward, normal);

            Vector3 localPlacement = splineTransform.InverseTransformPoint(placement);

            if (GetNearestPoint(currentSpline, localPlacement, out _, out _) < minDistanceFromSpline)
            {
                return false;
            }

            Debug.DrawRay(point, normal * offsetFromWall, Color.green, 5f);
            Debug.DrawRay(placement, forward * tangentDistance, Color.red, 5f);

            currentSpline.Add(new(localPlacement, -tangentDistance * forward, tangentDistance * forward), TangentMode.Mirrored);

            currentExtrude.Rebuild();
            currentInstantiate.UpdateInstances();

            return true;
        }

        private void InstantiateSplineContainer(Vector3 direction, Vector3 point, Vector3 normal)
        {
            Vector3 forward = Vector3.ProjectOnPlane(direction, normal).normalized;
            Vector3 placement = point + (normal * offsetFromWall);

            GameObject clone = Instantiate(railPrefab, placement, Quaternion.identity);

            splineTransform = clone.transform;

            SplineContainer container = clone.GetComponent<SplineContainer>();
            currentExtrude = clone.GetComponent<SplineExtrude>();
            currentInstantiate = clone.GetComponent<SplineInstantiate>();

            currentSpline = container.Spline;

            currentSpline[0] = new(-forward, -forward * tangentDistance, forward * tangentDistance);
            currentSpline[1] = new(forward, -forward * tangentDistance, forward * tangentDistance);

            clone.GetComponent<MeshFilter>().mesh = new Mesh();

            currentExtrude.Rebuild();
            currentInstantiate.UpdateInstances();

            RailManager.Instance.UpdateSplineInstantiators();
        }
    }
}