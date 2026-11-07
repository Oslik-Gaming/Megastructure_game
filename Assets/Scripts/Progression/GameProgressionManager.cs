using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Progression
{
    /// <summary>
    /// Главный менеджер прогрессии игры. Управляет списком шагов,
    /// проверяет условия завершения и переключает задания.
    /// </summary>
    public class GameProgressionManager : MonoBehaviour
    {
        [SerializeField] private List<ProgressionStep> steps = new List<ProgressionStep>();
        
        private int currentStepIndex = 0;
        private bool isInitialized = false;

        public int CurrentStepIndex => currentStepIndex;
        public ProgressionStep CurrentStep => currentStepIndex < steps.Count ? steps[currentStepIndex] : null;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            // Загрузить сохранённый шаг
            currentStepIndex = GameProgress.LoadProgressionStep();
            
            // Проверка валидности
            if (currentStepIndex < 0 || currentStepIndex >= steps.Count)
            {
                Debug.LogWarning($"[GameProgressionManager] Сохранённый шаг {currentStepIndex} выходит за границы. Сброс на 0.");
                currentStepIndex = 0;
                GameProgress.SaveProgressionStep(currentStepIndex);
            }

            // Восстановить состояние всех предыдущих шагов
            for (int i = 0; i < currentStepIndex; i++)
            {
                RestoreStep(i);
            }

            // Активировать текущий шаг
            if (currentStepIndex < steps.Count)
            {
                ActivateStep(currentStepIndex);
            }

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || currentStepIndex >= steps.Count) return;

            var currentStep = steps[currentStepIndex];
            
            // Проверить завершение шага
            if (currentStep.CheckCompletion(this))
            {
                AdvanceStep();
            }
        }

        private void ActivateStep(int stepIndex)
        {
            if (stepIndex >= steps.Count) return;

            var step = steps[stepIndex];
            
            Debug.Log($"[GameProgressionManager] Активация шага {stepIndex}: {step.stepId}");

            // Инициализировать шаг
            step.Initialize(this);

            // Выполнить действия при входе
            step.Enter(this);
        }

        private void RestoreStep(int stepIndex)
        {
            if (stepIndex >= steps.Count) return;

            var step = steps[stepIndex];
            
            // Восстановить действия (идемпотентно)
            step.Restore(this);
        }

        private void AdvanceStep()
        {
            currentStepIndex++;
            GameProgress.SaveProgressionStep(currentStepIndex);

            if (currentStepIndex < steps.Count)
            {
                ActivateStep(currentStepIndex);
            }
            else
            {
                Debug.Log("[GameProgressionManager] Все шаги завершены!");
            }
        }

        /// <summary>Сбросить прогрессию на начало (для "Новой игры").</summary>
        public void ResetProgression()
        {
            currentStepIndex = 0;
            GameProgress.ResetProgressionStep();
            isInitialized = false;
            Initialize();
        }
    }
}
