using System;
namespace TechCosmos.GBF.Runtime
{
    public class ContinuityMode<T> : ExecutionMode<T> where T : class
    {
        private bool _isFirst = true;
        public override void Execution(Action<T> applyAction)
        {
            if (IsEligible())
            {
                applyAction?.Invoke(target);
                _isFirst = false;
            }
        }

        public override bool IsEligible() => _isFirst;
    }
}

