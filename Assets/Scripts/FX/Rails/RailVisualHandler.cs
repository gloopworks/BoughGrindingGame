using UnityEngine;
using UnityEngine.Splines;

namespace MixJam12.FX.Rails
{
    public class RailVisualHandler : MonoBehaviour
    {
        [SerializeField] private SplineExtrude extrude;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        [Space]

        [SerializeField, Range(0.0f, 100.0f)] private float growthSpeed = 10f;

        private Spline spline;
        private float currentLength;
        private float targetLength;

        public void InitializeVisual(Spline spline)
        {
            this.spline = spline;

            meshFilter.mesh = new Mesh();
            currentLength = 0.0f;
            targetLength = spline.GetLength();

            UpdateVisual();
        }

        public void UpdateVisual()
        {
            extrude.Rebuild();
            targetLength = spline.GetLength();
        }

        private void Update()
        {
            if (meshRenderer != null && currentLength < targetLength)
            {
                currentLength = Mathf.MoveTowards(currentLength, targetLength, Time.deltaTime * growthSpeed);
                meshRenderer.materials[0].SetFloat("_Length", currentLength);
            }
        }
    }
}