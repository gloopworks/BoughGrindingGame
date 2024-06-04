using UnityEngine;
using UnityEngine.Splines;

namespace MixJam12.Gameplay.Rails
{
    public class RailManager : MonoBehaviour
    {
        public class RailTriggerEventArgs : System.EventArgs
        {
            public SplineInstantiate Instantiator { get; private set; }
            public SplineContainer Container => Instantiator.Container;

            public RailTriggerEventArgs(SplineInstantiate instantiator)
            {
                Instantiator = instantiator;
            }
        }

        public static RailManager Instance { get; private set; }
        private SplineInstantiate[] splineInstantiators;

        public event System.EventHandler<RailTriggerEventArgs> OnRailTriggerEnteredEvent;
        public event System.EventHandler<RailTriggerEventArgs> OnRailTriggerExitedEvent;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            UpdateSplineInstantiators();
        }

        public void UpdateSplineInstantiators()
        {
            splineInstantiators = FindObjectsOfType<SplineInstantiate>();
        }

        public void OnRailEntered(Transform root)
        {
            SplineInstantiate instantiator = FindSplineInstantiate(root);
            OnRailTriggerEnteredEvent?.Invoke(this, new RailTriggerEventArgs(instantiator));
        }

        public void OnRailExited(Transform root)
        {
            SplineInstantiate instantiator = FindSplineInstantiate(root);
            OnRailTriggerExitedEvent?.Invoke(this, new RailTriggerEventArgs(instantiator));
        }

        private SplineInstantiate FindSplineInstantiate(Transform root)
        {
            int instance = int.Parse(root.name[5..]);

            for (int i = 0; i < splineInstantiators.Length; i++)
            {
                if (instance == splineInstantiators[i].GetInstanceID())
                {
                    return splineInstantiators[i];
                }
            }

            return null;
        }
    }
}