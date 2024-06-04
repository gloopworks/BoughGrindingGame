using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem;

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

        [Header("Placement")]
        [SerializeField] private float offsetFromWall = 0.5f;
        [SerializeField] private float tangentDistance = 2f;

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
                InstantiateSplineContainer(ray.direction, hit.point, hit.normal);
            }
        }

        private void InstantiateSplineContainer(Vector3 direction, Vector3 point, Vector3 normal)
        {
            Vector3 forward = Vector3.ProjectOnPlane(direction, normal).normalized;
            Vector3 placement = point + (normal * offsetFromWall);

            Debug.DrawRay(point, normal * offsetFromWall, Color.green, 5f);
            Debug.DrawRay(placement, forward * tangentDistance, Color.red, 5f);

            GameObject clone = Instantiate(railPrefab, placement, Quaternion.identity);
            SplineContainer container = clone.GetComponent<SplineContainer>();
            SplineExtrude extrude = clone.GetComponent<SplineExtrude>();
            SplineInstantiate instantiate = clone.GetComponent<SplineInstantiate>();

            Spline spline = container.Spline;

            spline[0] = new(-forward, -forward * tangentDistance, forward * tangentDistance);
            spline[1] = new(forward, -forward * tangentDistance, forward * tangentDistance);

            clone.GetComponent<MeshFilter>().mesh = new Mesh();

            extrude.Rebuild();
            instantiate.SetDirty();
            instantiate.UpdateInstances();

            RailManager.Instance.UpdateSplineInstantiators();
        }
    }
}