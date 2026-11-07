using System;
using UnityEngine;

namespace Progression.Actions
{
    [Serializable]
    public class SwitchLocationTrackingAction : IStepAction
    {
        [SerializeField] private TrashLocation newLocation;

        public void Execute(GameProgressionManager context)
        {
            SwitchLocation();
        }

        public void Restore(GameProgressionManager context)
        {
            SwitchLocation();
        }

        private void SwitchLocation()
        {
            var gameHud = GameObject.FindFirstObjectByType<GameHud>();
            if (gameHud != null && newLocation != null)
            {
                gameHud.SwitchLocation(newLocation);
            }
            else
            {
                Debug.LogWarning("[SwitchLocationTrackingAction] GameHud или новая локация не найдены.");
            }
        }
    }
}
