using System;
using UnityEngine;

namespace Progression
{
    /// <summary>
    /// Компонент для триггер-зоны входа в локацию.
    /// Срабатывает при входе объекта с тегом "Player".
    /// </summary>
    public class ProgressionTriggerZone : MonoBehaviour
    {
        public event Action PlayerEntered;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerEntered?.Invoke();
            }
        }
    }
}
