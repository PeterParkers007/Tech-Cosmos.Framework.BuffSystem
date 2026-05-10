// Runtime/ExecutionMode.cs
using System;
using UnityEngine;

namespace TechCosmos.GBF.Runtime
{
    // ===== 基类 =====
    [Serializable]
    public abstract class ExecutionModeBase
    {
        [NonSerialized] public object target;
        [NonSerialized] public BuffContextBase context;
        public abstract bool IsEligible();
        public abstract void MarkExecuted();
    }

    [Serializable]
    public abstract class ExecutionMode<T> : ExecutionModeBase where T : class
    {
        public T TypedTarget => target as T;
        public BuffContext<T> TypedContext => context as BuffContext<T>;
    }

    // ===== 一次性 =====
    [Serializable]
    public sealed class ContinuityMode : ExecutionMode<object>
    {
        [SerializeField] private bool _executed;
        public override bool IsEligible() => !_executed;
        public override void MarkExecuted() => _executed = true;
    }

    // ===== 周期性 =====
    [Serializable]
    public sealed class PeriodicityMode : ExecutionMode<object>
    {
        [SerializeField] private float _interval = 1f;
        [SerializeField] private float _nextTime;

        public float Interval => _interval;
        public PeriodicityMode() { }
        public PeriodicityMode(float interval) => _interval = interval;

        public override bool IsEligible() => Time.time >= _nextTime;
        public override void MarkExecuted() => _nextTime = Time.time + _interval;
    }
}