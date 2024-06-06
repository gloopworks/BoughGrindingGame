using UnityEngine;
using UnityEngine.InputSystem;

namespace MixJam12.Utilities
{
    public static class GeneralUtils
    {
        public static string PlayerInputTag { get; } = "PlayerInput";

        private static PlayerInput cachedPlayerInput;

        public static PlayerInput GetPlayerInput()
        {
            if (cachedPlayerInput == null)
            {
                cachedPlayerInput = GameObject.FindWithTag(PlayerInputTag).GetComponent<PlayerInput>();
            }

            return cachedPlayerInput;
        }

        public static Vector3 ExcludeYAxis(this Vector3 input)
        {
            return new Vector3(input.x, 0f, input.z);
        }

        public static float SqrDistance(Vector3 target, Vector3 current)
        {
            return (target - current).sqrMagnitude;
        }

        public static float Remap(this float input, float inputMin, float inputMax, float outputMin, float outputMax)
        {
            return (input - inputMin) / (inputMax - inputMin) * (outputMax - outputMin) + outputMin;
        }

        public static float Remap01(this float input, float inputMin, float inputMax)
        {
            return Remap(input, inputMin, inputMax, 0.0f, 1.0f);
        }
    }
}