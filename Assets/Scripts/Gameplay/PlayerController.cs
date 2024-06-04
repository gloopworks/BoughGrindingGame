using UnityEngine;
using UnityEngine.InputSystem;

using static MixJam12.Utilities.GeneralUtils;

namespace MixJam12.Gameplay
{
    public abstract class PlayerController : MonoBehaviour
    {
        protected PlayerInput playerInput;

        public virtual void Start()
        {
            playerInput = GetPlayerInput();
            SubscribeToInputActions();
        }

        public abstract void SubscribeToInputActions();

        public abstract void UnsubscribeFromInputActions();
    }
}