using UnityEngine;
using UnityEngine.InputSystem;

namespace MixJam12.Gameplay
{
    public class ScreenClicker : PlayerController
    {
        public class OnScreenClickedEventArgs : System.EventArgs
        {
            public Vector3 Direction { get; private set; }
            public Vector3 Point { get; private set; }
            public Vector3 Normal { get; private set; }

            public OnScreenClickedEventArgs(Vector3 direction, Vector3 point, Vector3 normal)
            {
                Direction = direction;
                Point = point;
                Normal = normal;
            }
        }

        [SerializeField] private Camera mainCamera;
        private Vector2 screenMousePos;

        public event System.EventHandler<OnScreenClickedEventArgs> OnScreenClickedEvent;

        public override void SubscribeToInputActions()
        {
            playerInput.actions["Player/MouseLook"].performed += OnMouseLook;
            playerInput.actions["Player/Fire"].performed += OnFire;
        }

        private void OnFire(InputAction.CallbackContext ctx)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenMousePos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                OnScreenClickedEvent?.Invoke(this, new OnScreenClickedEventArgs(ray.direction, hit.point, hit.normal));
            }
        }

        private void OnMouseLook(InputAction.CallbackContext ctx)
        {
            screenMousePos = ctx.ReadValue<Vector2>();
        }

        public override void UnsubscribeFromInputActions()
        {
            playerInput.actions["Player/MouseLook"].performed -= OnMouseLook;
            playerInput.actions["Player/Fire"].performed -= OnFire;
        }
    }
}