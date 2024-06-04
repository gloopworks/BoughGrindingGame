using UnityEngine;

using MixJam12.Gameplay.Player;
namespace MixJam12.FX
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private PlayerMovement playerMovement;
        
        [Space]

        [SerializeField] private float standingOffset = 1f;

        [Space]

        [SerializeField, Range(0f, 100f)] private float crouchSpeed = 2f;
        [SerializeField] private AnimationCurve crouchCurve;
        
        private float localPosition
        {
            get
            {
                return transform.localPosition.y;
            }
            set
            {
                transform.localPosition = new Vector3(transform.localPosition.x, value, transform.localPosition.z);
            }
        }

        private float targetOffset;
        private float startingOffset;

        private float t;
        private void Start()
        {
            playerMovement.OnCrouchEvent += OnCrouch;
            
            targetOffset = standingOffset;
        }

        private void OnCrouch(object sender, PlayerMovement.OnCrouchEventArgs args)
        {
            targetOffset = args.IsCrouched ? (standingOffset - args.HeightDifference) / 2f : standingOffset;
            startingOffset = localPosition;
            t = 0f;
        }

        private void Update()
        {
            if (t < 1f)
            {
                localPosition = Mathf.Lerp(startingOffset, targetOffset, crouchCurve.Evaluate(t));
                t += Time.deltaTime * crouchSpeed;
            }
        }
    }
}