using System;
using System.Collections.Generic;
using UnityEngine;
namespace TechCosmos.GBF.Runtime
{
    public abstract class BaseBuff<T> : IBuff<T> where T : class
    {
        public bool isOver { get; set; }
        public int priority {  get; set; }
        public string[] tags { get; set; }
        public T target {  get; set; }

        public event Action<T> OnApply;
        public event Action<T> OnRemove;

        protected List<BuffEffectExecuter<T>> _buffEffectExecuters = new();

        protected float _duration;
        protected float _timer;
        protected bool _isPaused;
        protected bool _isTimePaused;
        protected float _timeScale = 1f;  // 时间缩放系数
        public BaseBuff(T target,float duration, string[] tags = default)
        {
            this.target = target;
            isOver = false;
            _duration = duration;
            _timer = 0f;
            _isPaused = false;
            _timeScale = 1f;
            this.tags = tags;
        }
        public void AddEffectExecuter(BuffEffectExecuter<T> executer) => _buffEffectExecuters.Add(executer);
        public void AddEffectExecuter(params BuffEffectExecuter<T>[] executers) => _buffEffectExecuters.AddRange(executers);
        public void RemoveEffectExecuter(BuffEffectExecuter<T> executer) => _buffEffectExecuters.Remove(executer);
        public void Apply()
        {
            foreach (var effect in _buffEffectExecuters) effect.Apply(target);
        }
        public void Remove()
        {
            foreach (var effect in _buffEffectExecuters) 
                if (effect is IRollBack rollBack) rollBack.RollBack();
            _buffEffectExecuters.Clear();
            TriggerRemoveEvent(target);
        }
        public void TriggerApplyEvent(T target) => OnApply?.Invoke(target);
        public void TriggerRemoveEvent(T target) => OnRemove?.Invoke(target);
        public void Update(float deltaTime)
        {
            if (_isPaused) return;

            // 使用独立的计时器
            if(!_isTimePaused) _timer += deltaTime * _timeScale;

            Apply();

            // 检查是否过期
            if (_timer >= _duration)
            {
                Remove();  // 自动清理效果
                isOver = true;
            }
        }
        // 完整的暂停/恢复系统
        public void Pause() => _isPaused = true;
        public void TimePause() => _isTimePaused = true;
        public void Resume() => _isPaused = false;
        public void TimeResume() => _isTimePaused = false;
        public void SetPaused(bool paused) => _isPaused = paused;
        public void SetTimePaused(bool paused) => _isTimePaused = paused;
        // 时间缩放控制
        public void SetTimeScale(float scale) => _timeScale = Mathf.Max(0, scale);  // 防止负值

        public float TimeScale => _timeScale;

        // 查询接口
        public float RemainingTime => Mathf.Max(0, _duration - _timer);
        public float ElapsedTime => _timer;
        public float Progress => _duration > 0 ? Mathf.Clamp01(_timer / _duration) : 1f;
        public bool IsPaused => _isPaused;

        // 刷新/重置计时器
        public void Refresh()
        {
            _timer = 0f;  // 重新开始计时
            isOver = false;
        }

        public void ResetTimer() => _timer = 0f;

        // 延长持续时间
        public void ExtendDuration(float extraTime) => _duration += extraTime;

        // 设置剩余时间（用于精确控制）
        public void SetRemainingTime(float remaining) => _timer = Mathf.Max(0, _duration - remaining);

    }
}

