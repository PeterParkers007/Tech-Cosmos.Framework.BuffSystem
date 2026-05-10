// ============================================================
// 文件：BuffStackPolicy.cs
// 路径：TechCosmos.GBF.Runtime/BuffStackPolicy.cs
// ============================================================
namespace TechCosmos.GBF.Runtime
{
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