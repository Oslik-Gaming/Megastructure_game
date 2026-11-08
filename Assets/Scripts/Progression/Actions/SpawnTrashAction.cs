using System;
using UnityEngine;

namespace Progression.Actions
{
    /// <summary>
    /// Спавнит начальный мусор при входе в шаг прогрессии.
    /// При восстановлении шага ничего не делает, поэтому Continue не создаёт
    /// новые экземпляры мусора.
    /// </summary>
    [Serializable]
    public class SpawnTrashAction : IStepAction
    {
        [SerializeField] private RandomTrashSpawner spawner;

        public void Execute(GameProgressionManager context)
        {
            if (spawner == null)
            {
                Debug.LogWarning("[SpawnTrashAction] Не указан RandomTrashSpawner.");
                return;
            }

            if (GameProgress.InitialTrashSpawned)
            {
                if (!spawner.HasSpawnedTrash)
                    spawner.SpawnWithSeed(GameProgress.InitialTrashSeed);
                return;
            }

            spawner.SpawnWithSeed(GameProgress.GetOrCreateInitialTrashSeed());
            GameProgress.MarkInitialTrashSpawned();
        }

        public void Restore(GameProgressionManager context)
        {
            // Сцена была выгружена при переходе в меню, поэтому создаём тот же
            // набор заново по сохранённому seed. Повторный вызов в одной сцене
            // ничего не делает.
            if (!GameProgress.InitialTrashSpawned || spawner == null || spawner.HasSpawnedTrash)
                return;

            spawner.SpawnWithSeed(GameProgress.InitialTrashSeed);
        }
    }
}
