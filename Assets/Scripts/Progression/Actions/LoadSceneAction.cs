using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Progression.Actions
{
    [Serializable]
    public class LoadSceneAction : IStepAction
    {
        [SerializeField] private string sceneName;

        public void Execute(GameProgressionManager context)
        {
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("[LoadSceneAction] Имя сцены не указано.");
            }
        }

        public void Restore(GameProgressionManager context)
        {
            // Загрузка сцены не восстанавливается
        }
    }
}
