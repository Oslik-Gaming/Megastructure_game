using System;
using UnityEngine;

namespace Progression.Conditions
{
    [Serializable]
    public class ObjectDestroyedCondition : IStepCondition
    {
        [SerializeField] private DamageableTrashItem target;

        public void Initialize(GameProgressionManager context)
        {
            // Не требуется инициализация
        }

        public bool IsCompleted(GameProgressionManager context)
        {
            return target == null || target.IsRemoved;
        }
    }
}
