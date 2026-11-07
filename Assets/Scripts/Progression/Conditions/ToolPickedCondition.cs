using System;
using UnityEngine;

namespace Progression.Conditions
{
    [Serializable]
    public class ToolPickedCondition : IStepCondition
    {
        [SerializeField] private ToolType toolType;

        public void Initialize(GameProgressionManager context)
        {
            // Не требуется инициализация
        }

        public bool IsCompleted(GameProgressionManager context)
        {
            var toolInventory = GameObject.FindFirstObjectByType<ToolInventory>();
            return toolInventory != null && toolInventory.HasTool(toolType);
        }
    }
}
