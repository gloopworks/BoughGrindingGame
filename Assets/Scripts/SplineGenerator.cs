using UnityEngine;
using UnityEngine.Splines;

using MixJam12.Gameplay;

namespace MixJam12
{
    public class SplineGenerator : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField] private SplineExtrude splineExtrude;
        [SerializeField] private ScreenClicker screenClicker;

        [Space]

        [SerializeField] private float tangentDistance = 1f;

        private void Awake()
        {
            splineExtrude.enabled = false;
        }

        private void Start()
        {
            screenClicker.OnScreenClickedEvent += OnScreenClicked;
        }

        private void OnScreenClicked(object sender, ScreenClicker.OnScreenClickedEventArgs args)
        {
            AddKnotToSpline(args.Direction, args.Point, args.Normal);
        }

        private void AddKnotToSpline(Vector3 direction, Vector3 point, Vector3 normal)
        {
            bool newSpline = TryAddSpline(out Spline spline);

            Vector3 position = point;
            if (!newSpline)
            {
                direction = position - (Vector3)spline[^1].Position;
            }

            direction = Vector3.ProjectOnPlane(direction, normal).normalized;

            spline.Add(new(position, -tangentDistance * direction, tangentDistance * direction), TangentMode.Continuous);
            splineExtrude.Rebuild();
        }

        private bool TryAddSpline(out Spline spline)
        {
            if (splineContainer.Splines.Count > 0)
            {
                spline = splineContainer.Splines[0];
                return false;
            }

            spline = splineContainer.AddSpline();
            splineExtrude.enabled = true;
            return true;
        }
    }
}