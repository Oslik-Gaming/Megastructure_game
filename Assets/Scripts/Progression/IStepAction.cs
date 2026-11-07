using System;

namespace Progression
{
    public interface IStepAction
    {
        void Execute(GameProgressionManager context);
        void Restore(GameProgressionManager context);
    }
}
