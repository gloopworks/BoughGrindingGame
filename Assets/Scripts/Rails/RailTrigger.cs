using UnityEngine;

namespace MixJam12.Gameplay.Rails
{
    public class RailTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider collider)
        {
            RailManager.Instance.OnRailEntered(transform.parent);
        }

        private void OnTriggerExit(Collider collider)
        {
            RailManager.Instance.OnRailEntered(transform.parent);
        }
    }
}