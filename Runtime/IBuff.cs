// ============================================================
// ÎÄ¼þ£ºIBuff.cs
// Â·¾¶£ºTechCosmos.GBF.Runtime/IBuff.cs
// ============================================================
using System;

namespace TechCosmos.GBF.Runtime
{
    public interface IBuff<T> where T : class
    {
        T target { get; set; }
        int priority { get; set; }
        bool isOver { get; set; }
        string[] tags { get; set; }

        string BuffName { get; }
        BuffStackPolicy StackPolicy { get; }
        int MaxStacks { get; }
        int CurrentStacks { get; set; }

        void TriggerApplyEvent(T target);
        void TriggerRemoveEvent(T target);
        void Apply();
        void Remove();
        void Update(float deltaTime);
        void Refresh();

        event Action<T> OnApply;
        event Action<T> OnRemove;

        void RegisterModifier(string modifyType, Func<float, BuffModifyContext<T>, float> modifier);
        float ModifyValue(string modifyType, float baseValue, BuffModifyContext<T> context = null);

        void RegisterAction(string actionName, Action<string, T, float, string> action);
        void OnAction(string actionName, T unit = null, float value = default, string damageType = default);
    }
}