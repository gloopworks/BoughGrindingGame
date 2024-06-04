using UnityEngine;

namespace MixJam12.Gameplay.Player
{
    [RequireComponent(typeof(Collider))]
    public class CollisionCheck : MonoBehaviour
    {
        public class OnHitGroundEventArgs : System.EventArgs
        {
            public bool OnGround { get; private set; }
            public bool OnSlope { get; private set; }

            public OnHitGroundEventArgs(bool onGround, bool onSlope)
            {
                OnGround = onGround;
                OnSlope = onSlope;
            }
        }

        [Tooltip("If a collision's normal (on the Y axis) is above this value, the surface will be considered ground")]
        [SerializeField, Range(0f, 1f)] private float groundNormalThreshold = 0.9f;
        [Tooltip("If a collision's normal (on the Y axis) is above this value, and below the ground threshold, the surface will be considered a slope")]
        [SerializeField, Range(0f, 1f)] private float slopeNormalThreshold = 0.4f;
        public bool OnGround { get; private set; }
        public bool OnSlope { get; private set; }
        public bool OnSurface => OnGround || OnSlope;
        public Vector3 SlopeNormal { get; private set; }

        public event System.EventHandler<OnHitGroundEventArgs> OnHitGroundEvent;

        private bool EvaluateCollision(Collision collision, out bool onSlope, out bool onGround)
        {
            onGround = false;
            onSlope = false;

            float yNormal = 0;

            for (int i = 0; i < collision.contactCount; i++)
            {
                yNormal = collision.contacts[i].normal.y;

                onGround |= yNormal >= groundNormalThreshold;
                onSlope |= yNormal >= slopeNormalThreshold && yNormal < groundNormalThreshold;

                SlopeNormal = onSlope ? collision.contacts[i].normal : Vector3.up;
            }

            return onGround || onSlope;
        }

        private void OnCollisionEnter(Collision collision)
        {
            bool landed = EvaluateCollision(collision, out bool onSlope, out bool onGround);
            if (!OnGround && landed)
            {
                OnHitGroundEvent?.Invoke(this, new(onGround, onSlope));
            }
            OnGround |= onGround;
            OnSlope |= onSlope;
        }

        private void OnCollisionStay(Collision collision)
        {
            _ = EvaluateCollision(collision, out bool onSlope, out bool onGround);
            OnGround |= onGround;
            OnSlope |= onSlope;
        }

        private void OnCollisionExit(Collision collision)
        {
            OnGround = false;
            OnSlope = false;
            SlopeNormal = Vector3.up;
        }
    }
}