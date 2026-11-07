using System;
using UnityEngine;

namespace Progression.Actions
{
    [Serializable]
    public class OpenDoorAction : IStepAction
    {
        [SerializeField] private Door door;

        public void Execute(GameProgressionManager context)
        {
            if (door != null)
            {
                door.Open();
            }
            else
            {
                Debug.LogWarning("[OpenDoorAction] Дверь не указана.");
            }
        }

        public void Restore(GameProgressionManager context)
        {
            if (door != null && !door.IsOpen)
            {
                door.Open();
            }
        }
    }
}
