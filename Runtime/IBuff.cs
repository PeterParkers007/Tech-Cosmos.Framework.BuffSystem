using System;

namespace TechCosmos.GBF.Runtime
{
    public interface IBuff<T> where T : class
    {
        T target { get; set; }
        int priority { get; set; }
        bool isOver { get; set; }
        string[] tags { get; set; }

        // 堆叠
        string BuffName { get; }
        BuffStackPolicy StackPolicy { get; }
        int MaxStacks { get; }
        int CurrentStacks { get; set; }

        // 生命周期
        void TriggerApplyEvent(T target);
        void TriggerRemoveEvent(T target);
        void Apply();
        void Remove();
        void Update(float deltaTime);
        void Refresh();

        event Action<T> OnApply;
        event Action<T> OnRemove;

        // 属性修改（字符串驱动）
        void RegisterModifier(string modifyType, Func<float, float> modifier);
        float ModifyValue(string modifyType, float baseValue);

        // 事件响应（字符串驱动）
        void RegisterAction(string actionName, Action<string, T, float, string> action);
        void OnAction(string actionName, T unit = null, float value = default, string damageType = default);
    }

    public enum BuffStackPolicy
    {
        /// <summary>刷新持续时间，不叠加层数</summary>
        ExtendDuration,
        /// <summary>叠加层数并刷新持续时间</summary>
        StackAndRefresh,
        /// <summary>每层独立计时</summary>
        Independent,
        /// <summary>新的替换旧的</summary>
        Replace
    }
}