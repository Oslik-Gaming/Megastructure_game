using System;
using UnityEngine;

namespace Progression.Actions
{
    [Serializable]
    public class SpawnToolAction : IStepAction
    {
        [SerializeField] private ToolType toolType;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject pickupPrefab;

        public void Execute(GameProgressionManager context)
        {
            SpawnTool();
        }

        public void Restore(GameProgressionManager context)
        {
            // Проверить, подобран ли инструмент
            var toolInventory = GameObject.FindFirstObjectByType<ToolInventory>();
            if (toolInventory != null && toolInventory.HasTool(toolType))
            {
                // Инструмент уже подобран, не спавнить
                return;
            }

            // Инструмент ещё не подобран, заспавнить заново
            SpawnTool();
        }

        private void SpawnTool()
        {
            if (pickupPrefab == null || spawnPoint == null)
            {
                Debug.LogWarning($"[SpawnToolAction] Не указан префаб или точка спавна для {toolType}");
                return;
            }

            GameObject toolObject = GameObject.Instantiate(pickupPrefab, spawnPoint.position, spawnPoint.rotation);
            var pickupable = toolObject.GetComponent<PickupableTool>();
            if (pickupable == null)
            {
                Debug.LogError($"[SpawnToolAction] Префаб {pickupPrefab.name} не содержит компонент PickupableTool");
            }
        }
    }
}
