// ============================================================
// ÎÄ¼þ£ºBuffModifyContext.cs
// Â·¾¶£ºTechCosmos.GBF.Runtime/BuffModifyContext.cs
// ============================================================
namespace TechCosmos.GBF.Runtime
{
    public class BuffModifyContext<T> where T : class
    {
        public T target;
        public T caster;
    }
}