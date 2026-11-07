using System;

namespace Progression
{
    public interface IStepCondition
    {
        void Initialize(GameProgressionManager context);
        bool IsCompleted(GameProgressionManager context);
    }
}
