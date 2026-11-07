using System.Collections;
using System;
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

        [Header("Open Sound")]
        [Tooltip("Один общий AudioSource для звука открытия дверей. Можно назначить один и тот же источник нескольким дверям. " +
                 "Клип, громкость и настройки 3D-звука настраиваются на самом AudioSource. " +
                 "Пусто — берётся AudioSource с двери или родителя; если его нет — звук не играет.")]
        [SerializeField] private AudioSource openAudioSource;

        [SerializeField, HideInInspector] private string persistentId;

        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool isOpen = false;
        private Coroutine openRoutine;

        private void Start()
        {
            EnsurePersistentId();

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

            // Если дверь уже числится открытой в сохранении — это восстановление
            // состояния (например, OpenDoorAction.Restore при загрузке сцены),
            // а не живое открытие игроком: звук не играем.
            bool isRestore = WasOpened();

            isOpen = true;
            MarkAsOpened();

            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
            }

            openRoutine = StartCoroutine(OpenRoutine());

            if (!isRestore)
            {
                PlayOpenSound();
            }
        }

        private void PlayOpenSound()
        {
            if (openAudioSource == null)
                openAudioSource = GetComponent<AudioSource>();
            if (openAudioSource == null)
                openAudioSource = GetComponentInParent<AudioSource>();

            if (openAudioSource == null)
            {
                Debug.LogWarning($"[{name}] AudioSource для звука открытия двери не назначен и не найден.", this);
                return;
            }

            // Клип и настройки задаются на самом AudioSource — просто запускаем проигрывание.
            openAudioSource.Play();
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
            return $"Progress_Door_{persistentId}_Opened_Gen{GameProgress.ProgressGeneration}";
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
            Door door = doorObject != null ? doorObject.GetComponent<Door>() : null;
            if (door != null)
            {
                PlayerPrefs.DeleteKey(door.GetDoorKey());
            }
            PlayerPrefs.Save();
        }

        private void EnsurePersistentId()
        {
            if (!string.IsNullOrEmpty(persistentId))
                return;

            // Stable for this scene object once serialized by Unity.
            persistentId = Guid.NewGuid().ToString("N");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(persistentId) &&
                gameObject.scene.IsValid() &&
                !UnityEditor.EditorUtility.IsPersistent(gameObject))
            {
                persistentId = Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
