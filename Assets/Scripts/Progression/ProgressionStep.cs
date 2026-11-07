using System;
using System.Collections.Generic;
using UnityEngine;

namespace Progression
{
    [Serializable]
    public class ProgressionStep
    {
        public string stepId;
        public string taskText;
        
        [SerializeReference] public List<IStepAction> entryActions = new List<IStepAction>();
        [SerializeReference] public List<IStepCondition> conditions = new List<IStepCondition>();

        private bool isInitialized = false;

        public void Enter(GameProgressionManager context)
        {
            Debug.Log($"[ProgressionStep] Вход в шаг: {stepId}");
            
            if (entryActions != null)
            {
                foreach (var action in entryActions)
                {
                    action?.Execute(context);
                }
            }
        }

        public void Restore(GameProgressionManager context)
        {
            Debug.Log($"[ProgressionStep] Восстановление шага: {stepId}");
            
            if (entryActions != null)
            {
                foreach (var action in entryActions)
                {
                    action?.Restore(context);
                }
            }
        }

        public void Initialize(GameProgressionManager context)
        {
            if (isInitialized) return;

            if (conditions != null)
            {
                foreach (var condition in conditions)
                {
                    condition?.Initialize(context);
                }
            }

            isInitialized = true;
        }

        public bool CheckCompletion(GameProgressionManager context)
        {
            if (conditions == null || conditions.Count == 0)
                return false;

            foreach (var condition in conditions)
            {
                if (condition == null || !condition.IsCompleted(context))
                    return false;
            }

            return true;
        }
    }
}
