// ============================================================
// 文件：BuffEffectMenuAttribute.cs
// 路径：TechCosmos.GBF.Runtime/BuffEffectMenuAttribute.cs
// ============================================================
using System;

namespace TechCosmos.GBF.Runtime
{
    /// <summary>
    /// 标记 BuffEffect 在编辑器菜单中的分类和显示名
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class BuffEffectMenuAttribute : Attribute
    {
        public string Category { get; }
        public string DisplayName { get; set; }
        public int Priority { get; set; } = 99;

        public BuffEffectMenuAttribute(string category)
        {
            Category = category;
        }
    }
}