using System.Collections;
using UnityEngine;

namespace Progression
{
    /// <summary>
    /// Компонент двери, которая может открываться с анимацией сдвига.
    /// </summary>
    public class Door : MonoBehaviour
    {
        [SerializeField] private Transform doorModel;
        [SerializeField] private Vector3 openOffset = new Vector3(0, 3, 0);
        [SerializeField] private float openDuration = 1.5f;

        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool isOpen = false;
        private Coroutine openRoutine;

        private void Start()
        {
            if (doorModel != null)
            {
                closedPosition = doorModel.position;
                openPosition = closedPosition + openOffset;
            }

            // Восстановить состояние из сохранения
            if (WasOpened())
            {
                OpenImmediate();
            }
        }

        public void Open()
        {
            if (isOpen) return;

            isOpen = true;
            MarkAsOpened();

            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
            }

            openRoutine = StartCoroutine(OpenRoutine());
        }

        public void OpenImmediate()
        {
            if (doorModel != null)
            {
                doorModel.position = openPosition;
            }
            isOpen = true;
        }

        private IEnumerator OpenRoutine()
        {
            if (doorModel == null)
            {
                yield break;
            }

            float elapsed = 0f;
            Vector3 startPos = doorModel.position;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
                doorModel.position = Vector3.Lerp(startPos, openPosition, t);
                yield return null;
            }

            doorModel.position = openPosition;
            openRoutine = null;
        }

        public bool IsOpen => isOpen;

        private string GetDoorKey()
        {
            return $"Progress_Door_{gameObject.GetInstanceID()}_Opened";
        }

        private bool WasOpened()
        {
            return PlayerPrefs.GetInt(GetDoorKey(), 0) == 1;
        }

        private void MarkAsOpened()
        {
            PlayerPrefs.SetInt(GetDoorKey(), 1);
            PlayerPrefs.Save();
        }

        public static void ResetDoor(GameObject doorObject)
        {
            string key = $"Progress_Door_{doorObject.GetInstanceID()}_Opened";
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
