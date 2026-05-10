// ============================================================
// 文件：AutoGenerateBuffEffectAttribute.cs
// 路径：TechCosmos.GBF.Runtime/AutoGenerateBuffEffectAttribute.cs
// ============================================================
using System;

namespace TechCosmos.GBF.Runtime
{
    /// <summary>
    /// 标记需要为指定目标类型生成封闭 BuffEffect 的泛型基类
    /// 用法：[AutoGenerateBuffEffect(typeof(Character), typeof(Enemy))]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AutoGenerateBuffEffectAttribute : Attribute
    {
        public Type[] TargetTypes { get; }

        public AutoGenerateBuffEffectAttribute(params Type[] targetTypes)
        {
            TargetTypes = targetTypes;
        }
    }

    /// <summary>
    /// 标记该类型需要被 BuffEffect 生成器作为目标 T
    /// 例如 [ApplyBuffTarget] class Character { }
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ApplyBuffTargetAttribute : Attribute
    {
    }
    // Runtime/BuffFieldAttribute.cs
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class BuffFieldAttribute : Attribute
    {
    }
}