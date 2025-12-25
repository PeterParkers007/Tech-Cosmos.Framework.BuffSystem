using System;
namespace TechCosmos.GBF.Runtime
{
    public class PeriodicityMode<T> : ExecutionMode<T> where T : class
    {
        private float _intervalTime;
        private float _nextAvailableTime;
        public PeriodicityMode(float intervalTime) => _intervalTime = intervalTime;
        public override void Execution(Action<T> applyAction)
        {
            if (IsEligible())
            {
                applyAction?.Invoke(target);
                _nextAvailableTime = UnityEngine.Time.time + _intervalTime;
            }
        }

        public override bool IsEligible() => UnityEngine.Time.time >= _nextAvailableTime;
    }
}

