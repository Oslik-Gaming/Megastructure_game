using System;
using System.Collections;
using Effects;
using UnityEngine;

namespace Progression.Actions
{
    /// <summary>Entry Action для запуска взрыва реактора с задержкой.</summary>
    [Serializable]
    public class TriggerReactorExplosionAction : IStepAction
    {
        [SerializeField] private ReactorExplosionEffect reactor;
        [SerializeField, Min(0f)] private float delay = 0f;

        public void Execute(GameProgressionManager context)
        {
            if (reactor == null)
            {
                Debug.LogWarning("[TriggerReactorExplosionAction] Не указан ReactorExplosionEffect.");
                return;
            }

            reactor.StartCoroutine(DelayedPlay());
        }

        private IEnumerator DelayedPlay()
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (reactor != null)
                reactor.Play();
        }

        public void Restore(GameProgressionManager context)
        {
            // Взрыв не запускается повторно при восстановлении сохранённого шага.
        }
    }
}
