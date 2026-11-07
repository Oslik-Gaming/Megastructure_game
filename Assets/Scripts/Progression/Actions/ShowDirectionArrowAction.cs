using System;
using UnityEngine;

namespace Progression.Actions
{
    [Serializable]
    public class ShowDirectionArrowAction : IStepAction
    {
        [SerializeField] private Transform target;

        public void Execute(GameProgressionManager context)
        {
            var directionArrow = GameObject.FindFirstObjectByType<DirectionArrow>();
            if (directionArrow != null && target != null)
            {
                directionArrow.Show(target);
            }
            else
            {
                Debug.LogWarning("[ShowDirectionArrowAction] DirectionArrow или цель не найдены.");
            }
        }

        public void Restore(GameProgressionManager context)
        {
            Execute(context);
        }
    }
}
