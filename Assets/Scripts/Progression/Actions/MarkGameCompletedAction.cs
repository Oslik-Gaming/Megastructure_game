using System;
using UnityEngine;

namespace Progression.Actions
{
    [Serializable]
    public class MarkGameCompletedAction : IStepAction
    {
        public void Execute(GameProgressionManager context)
        {
            GameProgress.MarkGameCompleted();
            Debug.Log("[MarkGameCompletedAction] Игра отмечена как завершённая.");
        }

        public void Restore(GameProgressionManager context)
        {
            // Завершение фиксируется только при фактическом выполнении действия.
        }
    }
}
