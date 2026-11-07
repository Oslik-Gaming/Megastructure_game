using System;
using System.Collections;
using UnityEngine;

namespace Progression
{
    /// <summary>
    /// Компонент для инструмента, который можно подобрать в мире.
    /// Реализует IInteractable для взаимодействия с игроком.
    /// </summary>
    public class PickupableTool : MonoBehaviour, IInteractable
    {
        [SerializeField] private ToolType toolType;
        [SerializeField] private GameObject visualModel;
        [SerializeField] private float pickupAnimDuration = 0.5f;
        [SerializeField] private float hoverHeight = 0.3f;
        [SerializeField] private float hoverSpeed = 1f;

        private Vector3 startPosition;
        private bool isPickedUp = false;

        private void Start()
        {
            startPosition = transform.position;
            
            // Проверить, не был ли инструмент уже подобран
            if (WasPickedUp())
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isPickedUp && visualModel != null)
            {
                // Анимация левитации
                float hover = Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
                visualModel.transform.position = startPosition + Vector3.up * hover;
                visualModel.transform.Rotate(Vector3.up, 30f * Time.deltaTime);
            }
        }

        public void Interact()
        {
            if (isPickedUp) return;

            var toolInventory = FindFirstObjectByType<ToolInventory>();
            if (toolInventory == null)
            {
                Debug.LogError("[PickupableTool] ToolInventory не найден.");
                return;
            }

            isPickedUp = true;
            toolInventory.UnlockTool(toolType);

            var toolController = FindFirstObjectByType<ToolController>();
            if (toolController != null)
            {
                toolController.SelectTool(toolType);
            }
            else
            {
                Debug.LogWarning("[PickupableTool] ToolController не найден.");
            }

            MarkAsPickedUp();

            StartCoroutine(PickupAnimation());
        }

        private IEnumerator PickupAnimation()
        {
            if (visualModel == null)
            {
                Destroy(gameObject);
                yield break;
            }

            Vector3 startPos = visualModel.transform.position;
            Vector3 startScale = visualModel.transform.localScale;
            Vector3 endPos = startPos + Vector3.up * 1f;
            Vector3 endScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < pickupAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pickupAnimDuration;

                visualModel.transform.position = Vector3.Lerp(startPos, endPos, t);
                visualModel.transform.localScale = Vector3.Lerp(startScale, endScale, t);

                yield return null;
            }

            Destroy(gameObject);
        }

        private string GetPickupKey()
        {
            return $"Progress_Tool_{toolType}_Picked_Gen{GameProgress.ProgressGeneration}";
        }

        private bool WasPickedUp()
        {
            return PlayerPrefs.GetInt(GetPickupKey(), 0) == 1;
        }

        private void MarkAsPickedUp()
        {
            PlayerPrefs.SetInt(GetPickupKey(), 1);
            PlayerPrefs.Save();
        }

        public static void ResetPickup(ToolType toolType)
        {
            string key = $"Progress_Tool_{toolType}_Picked";
            key += $"_Gen{GameProgress.ProgressGeneration}";
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
