using System;
using UnityEngine;

namespace Progression.Actions
{
    [Serializable]
    public class HideDirectionArrowAction : IStepAction
    {
        public void Execute(GameProgressionManager context)
        {
            var directionArrow = GameObject.FindFirstObjectByType<DirectionArrow>();
            if (directionArrow != null)
            {
                directionArrow.Hide();
            }
        }

        public void Restore(GameProgressionManager context)
        {
            Execute(context);
        }
    }
}
