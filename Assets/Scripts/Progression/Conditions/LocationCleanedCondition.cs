using System;
using UnityEngine;

namespace Progression.Conditions
{
    [Serializable]
    public class LocationCleanedCondition : IStepCondition
    {
        [SerializeField] private TrashLocation location;

        public void Initialize(GameProgressionManager context)
        {
            // Не требуется инициализация
        }

        public bool IsCompleted(GameProgressionManager context)
        {
            return location != null && location.CleanedPercent >= 100f;
        }
    }
}
