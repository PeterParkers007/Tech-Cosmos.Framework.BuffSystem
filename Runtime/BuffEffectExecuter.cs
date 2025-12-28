using System;
using System.Collections.Generic;
namespace TechCosmos.GBF.Runtime
{
    public abstract class BuffEffectExecuter<T> where T : class
    {
        protected ExecutionMode<T> _executionMode;
        protected List<BuffEffect<T>> buffEffects = new();
        protected Action<T> _action;
        public BuffEffectExecuter(ExecutionMode<T> executionMode, params BuffEffect<T>[] buffEffects)
        {
            _executionMode = executionMode;
            this.buffEffects.AddRange(buffEffects);
            foreach (var effect in this.buffEffects) _action += effect.Effect;
        }
        public virtual void Apply(T target)
        {
            if (target == null) return; 
            _executionMode.target = target;
            _executionMode.Execution(_action);
            foreach (var effect in this.buffEffects) if (effect is IUpdate update) update.OnUpdate();
        }
    }
}
