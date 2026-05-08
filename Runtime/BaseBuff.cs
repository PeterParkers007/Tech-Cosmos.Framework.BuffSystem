using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.GBF.Runtime
{
    public abstract class BaseBuff<T> : IBuff<T> where T : class
    {
        public bool isOver { get; set; }
        public int priority { get; set; }
        public string[] tags { get; set; }
        public T target { get; set; }

        public event Action<T> OnApply;
        public event Action<T> OnRemove;

        protected List<BuffEffectExecuter<T>> _buffEffectExecuters = new();

        protected float _duration;
        protected float _timer;
        protected bool _isPaused;
        protected bool _isTimePaused;
        protected float _timeScale = 1f;

        // ===== 堆叠 =====
        public virtual string BuffName => GetType().Name;
        public virtual BuffStackPolicy StackPolicy => BuffStackPolicy.ExtendDuration;
        public virtual int MaxStacks => 1;
        public int CurrentStacks { get; set; } = 1;

        // ===== 属性修改（字符串驱动）=====
        protected Dictionary<string, Func<float, float>> _modifiers = new();

        // ===== 事件响应（字符串驱动）=====
        protected Dictionary<string, Action<string, T, float, string>> _actions = new();

        public BaseBuff(T target, float duration, string[] tags = null)
        {
            this.target = target;
            isOver = false;
            _duration = duration;
            _timer = 0f;
            _isPaused = false;
            _timeScale = 1f;
            this.tags = tags ?? Array.Empty<string>();
        }

        // ===== 执行器管理 =====
        public void AddEffectExecuter(BuffEffectExecuter<T> executer)
            => _buffEffectExecuters.Add(executer);

        public void AddEffectExecuter(params BuffEffectExecuter<T>[] executers)
            => _buffEffectExecuters.AddRange(executers);

        public void RemoveEffectExecuter(BuffEffectExecuter<T> executer)
            => _buffEffectExecuters.Remove(executer);

        // ===== 属性修改（字符串驱动）=====
        public void RegisterModifier(string modifyType, Func<float, float> modifier)
            => _modifiers[modifyType] = modifier;

        public virtual float ModifyValue(string modifyType, float baseValue)
        {
            if (_modifiers.TryGetValue(modifyType, out var modifier))
                return modifier(baseValue);
            return baseValue;
        }

        // ===== 事件响应（字符串驱动）=====
        public void RegisterAction(string actionName, Action<string, T, float, string> action)
            => _actions[actionName] = action;

        public virtual void OnAction(string actionName, T unit = null, float value = default, string damageType = default)
        {
            if (_actions.TryGetValue(actionName, out var action))
                action(actionName, unit, value, damageType);
        }

        // ===== 生命周期 =====
        public void Apply()
        {
            for (int i = 0; i < _buffEffectExecuters.Count; i++)
                _buffEffectExecuters[i].Apply(target);
        }

        public void Remove()
        {
            for (int i = 0; i < _buffEffectExecuters.Count; i++)
            {
                if (_buffEffectExecuters[i] is IRollBack rollBack)
                    rollBack.RollBack();
            }
            _buffEffectExecuters.Clear();
            TriggerRemoveEvent(target);
        }

        public void TriggerApplyEvent(T target) => OnApply?.Invoke(target);
        public void TriggerRemoveEvent(T target) => OnRemove?.Invoke(target);

        public void Update(float deltaTime)
        {
            if (_isPaused) return;

            if (!_isTimePaused)
                _timer += deltaTime * _timeScale;

            Apply();

            if (_timer >= _duration)
            {
                Remove();
                isOver = true;
            }
        }

        // ===== 时间控制 =====
        public void Pause() => _isPaused = true;
        public void TimePause() => _isTimePaused = true;
        public void Resume() => _isPaused = false;
        public void TimeResume() => _isTimePaused = false;
        public void SetPaused(bool paused) => _isPaused = paused;
        public void SetTimePaused(bool paused) => _isTimePaused = paused;
        public void SetTimeScale(float scale) => _timeScale = Mathf.Max(0, scale);
        public float TimeScale => _timeScale;

        public float RemainingTime => Mathf.Max(0, _duration - _timer);
        public float ElapsedTime => _timer;
        public float Progress => _duration > 0 ? Mathf.Clamp01(_timer / _duration) : 1f;
        public bool IsPaused => _isPaused;

        public void Refresh()
        {
            _timer = 0f;
            isOver = false;
        }

        public void ResetTimer() => _timer = 0f;
        public void ExtendDuration(float extraTime) => _duration += extraTime;
        public void SetRemainingTime(float remaining) => _timer = Mathf.Max(0, _duration - remaining);
    }
}