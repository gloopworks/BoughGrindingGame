using UnityEngine;
using UnityEngine.Splines;

public class SplineGenerator : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private SplineExtrude splineExtrude;

    [Space]

    [SerializeField] private float spread = 1f;

    private void Awake()
    {
        splineExtrude.enabled = false;
    }

    private void Start()
    {
        CreateSpline(Vector3.zero, Vector3.one, Vector3.up * 5f);
        splineExtrude.enabled = true;
        splineExtrude.Rebuild();
    }

    private void CreateSpline(params Vector3[] pathPoints)
    {
        Spline spline = splineContainer.AddSpline();

        BezierKnot[] knots = new BezierKnot[pathPoints.Length];
        for (int i = 0; i < pathPoints.Length; i++)
        {
            knots[i] = new BezierKnot
            (
                pathPoints[i],
                -spread * Vector3.forward,
                spread * Vector3.forward
            );
        }

        spline.Knots = knots;
    }
}
