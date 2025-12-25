using System;
namespace TechCosmos.GBF.Runtime
{
    public abstract class ExecutionMode<T> where T : class
    {
        public T target;
        public abstract bool IsEligible();
        public abstract void Execution(Action<T> applyAction);
    }
}
