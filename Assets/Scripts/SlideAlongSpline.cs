using UnityEngine;
using UnityEngine.Splines;
using static UnityEngine.Splines.SplineUtility;

public class SlideAlongSpline : MonoBehaviour
{
    [SerializeField] private SplineContainer spline;
    [SerializeField] private Transform reference;

    [Space, SerializeField, Range(0.0f, 1.0f)] float t;

    private void Update()
    {
        spline.Spline.Evaluate(t, out var fPos, out var fTan, out var fUp);

        reference.position = spline.transform.TransformPoint(fPos);
        reference.forward = fTan;
        reference.up = fUp;
    }
}
