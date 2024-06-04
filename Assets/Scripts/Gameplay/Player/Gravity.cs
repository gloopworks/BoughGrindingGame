using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixJam12.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class Gravity : MonoBehaviour
    {
        [SerializeField, Range(0f, 10f)] private float gravityScale = 1f;
        private Rigidbody body;
        [Space, SerializeField] private CollisionCheck collisionCheck;
        [SerializeField] private PlayerMovement playerMovement;

        private void Start()
        {
            body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (collisionCheck.OnSurface && !playerMovement.IsSliding)
            {
                return;
            }

            Vector3 gravity = gravityScale * -9.81f * Vector3.up;
            body.AddForce(gravity, ForceMode.Acceleration);
        }
    }
}