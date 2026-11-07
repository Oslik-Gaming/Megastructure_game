using System;
using UnityEngine;

namespace Progression.Actions
{
    [Serializable]
    public class ShowTaskAction : IStepAction
    {
        [SerializeField] private string taskText;

        public void Execute(GameProgressionManager context)
        {
            var gameHud = GameObject.FindFirstObjectByType<GameHud>();
            if (gameHud != null)
            {
                gameHud.SetTask(taskText);
            }
            else
            {
                Debug.LogWarning("[ShowTaskAction] GameHud не найден.");
            }
        }

        public void Restore(GameProgressionManager context)
        {
            Execute(context);
        }
    }
}
