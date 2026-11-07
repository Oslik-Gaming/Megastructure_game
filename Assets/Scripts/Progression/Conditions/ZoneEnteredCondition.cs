using System;
using UnityEngine;

namespace Progression.Conditions
{
    [Serializable]
    public class ZoneEnteredCondition : IStepCondition
    {
        [SerializeField] private Collider triggerZone;

        private bool entered = false;
        private ProgressionTriggerZone triggerComponent;

        public void Initialize(GameProgressionManager context)
        {
            entered = false;

            if (triggerZone != null)
            {
                triggerComponent = triggerZone.GetComponent<ProgressionTriggerZone>();
                if (triggerComponent == null)
                {
                    triggerComponent = triggerZone.gameObject.AddComponent<ProgressionTriggerZone>();
                }

                triggerComponent.PlayerEntered += OnPlayerEntered;
            }
        }

        public bool IsCompleted(GameProgressionManager context)
        {
            return entered;
        }

        private void OnPlayerEntered()
        {
            entered = true;
            
            if (triggerComponent != null)
            {
                triggerComponent.PlayerEntered -= OnPlayerEntered;
            }
        }
    }
}
