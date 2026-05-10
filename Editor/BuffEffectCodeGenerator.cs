#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TechCosmos.GBF.Runtime;

namespace TechCosmos.GBF.Editor
{
    public static class BuffEffectCodeGenerator
    {
        // 这两个路径对应技能系统的 Generated/Mechanisms 和 Generated/Conditions
        private const string EFFECT_GENERATED_FOLDER = "Assets/Generated/GBF/Effects";
        private const string MODE_GENERATED_FOLDER = "Assets/Generated/GBF/ExecutionModes";

        [MenuItem("Tech-Cosmos/GBF/Generate All BuffEffect Classes", priority = 20)]
        public static void GenerateAllBuffEffectClasses()
        {
            GenerateEffectClasses();
            GenerateExecutionModeClasses();
            Debug.Log("GBF: BuffEffect 和 ExecutionMode 类生成完成");
        }

        [MenuItem("Tech-Cosmos/GBF/Generate BuffEffect Classes", priority = 21)]
        public static void GenerateEffectClasses()
        {
            GenerateClasses(
                EFFECT_GENERATED_FOLDER,
                typeof(BuffEffect<>),
                "效果"
            );
        }

        [MenuItem("Tech-Cosmos/GBF/Generate ExecutionMode Classes", priority = 22)]
        public static void GenerateExecutionModeClasses()
        {
            GenerateClasses(
                MODE_GENERATED_FOLDER,
                typeof(ExecutionMode<>),
                "执行模式"
            );
        }

        private static void GenerateClasses(string outputFolder, Type genericBaseType, string typeLabel)
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .ToList();

            // 收集所有标记了 [AutoGenerateBuffEffect] 的开放泛型
            var genericTypes = CollectGenericTypes(allAssemblies, genericBaseType);

            // 收集所有注册的目标类型（如果用户标记了特定的 T）
            var targetTypes = CollectTargetTypes(allAssemblies);

            if (targetTypes.Count == 0)
            {
                // 如果没有任何类型注册，默认用常见类型
                targetTypes.Add(typeof(object));
                Debug.LogWarning($"[GBF Generator] 未找到任何注册的目标类型，默认使用 object");
            }

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            // 清理旧文件
            foreach (var file in Directory.GetFiles(outputFolder, "*.g.cs"))
                File.Delete(file);

            int count = 0;
            foreach (var (openGenericType, _) in genericTypes)
            {
                foreach (var targetType in targetTypes)
                {
                    try
                    {
                        var code = GenerateClassCode(openGenericType, targetType, outputFolder);
                        var fileName = $"{GetCleanName(targetType)}{GetCleanName(openGenericType)}.g.cs";
                        var filePath = Path.Combine(outputFolder, fileName);
                        File.WriteAllText(filePath, code, Encoding.UTF8);
                        count++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GBF Generator] 生成{typeLabel}失败 [{openGenericType.Name}<{targetType.Name}>]: {e.Message}");
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"GBF: 生成 {count} 个{typeLabel}类 -> {outputFolder}");
        }

        private static List<(Type type, Type[] targetTypes)> CollectGenericTypes(
            List<System.Reflection.Assembly> assemblies, Type genericBaseType)
        {
            var result = new List<(Type, Type[])>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsGenericTypeDefinition && !t.IsAbstract)
                        .Where(t =>
                        {
                            // 检查是否继承自泛型基类
                            var baseType = t.BaseType;
                            while (baseType != null)
                            {
                                if (baseType.IsGenericType &&
                                    baseType.GetGenericTypeDefinition() == genericBaseType)
                                    return true;
                                baseType = baseType.BaseType;
                            }
                            return false;
                        });

                    foreach (var type in types)
                    {
                        var targetTypesAttr = type.GetCustomAttributes(typeof(AutoGenerateBuffEffectAttribute), false)
                            .FirstOrDefault() as AutoGenerateBuffEffectAttribute;
                        var targets = targetTypesAttr?.TargetTypes ?? Array.Empty<Type>();
                        result.Add((type, targets));
                    }
                }
                catch { }
            }

            return result;
        }

        private static List<Type> CollectTargetTypes(List<System.Reflection.Assembly> assemblies)
        {
            var types = new HashSet<Type>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var marked = assembly.GetExportedTypes()
                        .Where(t => t.GetCustomAttributes(typeof(ApplyBuffTargetAttribute), false).Any());

                    foreach (var t in marked)
                        types.Add(t);
                }
                catch { }
            }

            return types.ToList();
        }

        private static string GenerateClassCode(Type openGenericType, Type targetType, string outputFolder)
        {
            var ns = openGenericType.Namespace ?? "TechCosmos.GBF.Runtime.Effects";
            var baseClassName = GetCleanName(openGenericType);
            var targetClassName = GetCleanName(targetType);
            var className = $"{targetClassName}{baseClassName}";

            // 判断是 Effect 还是 ExecutionMode
            bool isEffect = outputFolder.Contains("Effects");
            string baseClass = isEffect
                ? $"{baseClassName}<{targetClassName}>"
                : $"{baseClassName}<{targetClassName}>";

            var attr = openGenericType.GetCustomAttributes(typeof(BuffEffectMenuAttribute), false)
                .FirstOrDefault() as BuffEffectMenuAttribute;

            string attributeLine = "";
            if (attr != null)
            {
                string displayName = attr.DisplayName ?? baseClassName;
                attributeLine = $"\n    [BuffEffectMenu(\"{attr.Category}\", DisplayName = \"{displayName}\")]";
            }

            return $@"// <auto-generated/>
// 生成时间: {DateTime.Now:yyyy/MM/dd HH:mm:ss}
// 类型: {baseClassName}<{targetClassName}>
// 请勿手动修改此文件

using System;
using {ns};

namespace {ns}.Generated
{{
    [Serializable]{attributeLine}
    public sealed class {className} : {baseClass}
    {{
    }}
}}
";
        }

        private static string GetCleanName(Type type)
        {
            var name = type.Name;
            int backtick = name.IndexOf('`');
            if (backtick > 0)
                name = name.Substring(0, backtick);
            return name;
        }
    }
}
#endif