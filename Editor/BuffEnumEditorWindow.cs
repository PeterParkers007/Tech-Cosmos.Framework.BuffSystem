#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace TechCosmos.GBF.Editor
{
    public class BuffEnumEditorWindow : EditorWindow
    {
        private enum EditTarget { ModifyType, ActionName }

        private EditTarget target = EditTarget.ModifyType;
        private List<string> items = new();
        private string newItemName = "";
        private Vector2 scrollPos;
        private bool dirty;
        private string searchFilter = "";

        private const string MODIFY_ENUM_PATH = "Assets/Generated/GBF/BuffModifyType.cs";
        private const string ACTION_ENUM_PATH = "Assets/Generated/GBF/BuffActionType.cs";

        [MenuItem("Tech-Cosmos/GBF/Buff Enum Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<BuffEnumEditorWindow>("Buff 枚举编辑器");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        void OnEnable()
        {
            LoadFromFile(GetCurrentPath());
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Buff 枚举编辑器", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "管理 Buff 系统的 ModifyType 和 ActionName 枚举。\n" +
                "修改后点击「生成枚举文件」写入代码。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // 切换目标
            EditorGUILayout.BeginHorizontal();
            var newTarget = (EditTarget)EditorGUILayout.EnumPopup("编辑目标", target);
            if (newTarget != target)
            {
                target = newTarget;
                LoadFromFile(GetCurrentPath());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 添加新项
            EditorGUILayout.LabelField("添加新项", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            newItemName = EditorGUILayout.TextField(newItemName);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("添加", GUILayout.Width(60)) && !string.IsNullOrWhiteSpace(newItemName))
            {
                var name = SanitizeName(newItemName);
                if (!items.Contains(name))
                {
                    items.Add(name);
                    newItemName = "";
                    dirty = true;
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", $"'{name}' 已存在", "确定");
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 搜索
            var searchStyle = new GUIStyle("SearchTextField");
            searchFilter = EditorGUILayout.TextField("搜索", searchFilter, searchStyle);

            // 已有列表
            EditorGUILayout.LabelField($"已有项 ({items.Count})", EditorStyles.boldLabel);

            var filtered = string.IsNullOrEmpty(searchFilter)
                ? items
                : items.FindAll(i => i.ToLower().Contains(searchFilter.ToLower()));

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));

            for (int i = 0; i < filtered.Count; i++)
            {
                int realIndex = items.IndexOf(filtered[i]);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"{realIndex + 1}.", GUILayout.Width(30));
                string newName = EditorGUILayout.TextField(items[realIndex]);
                if (newName != items[realIndex])
                {
                    items[realIndex] = SanitizeName(newName);
                    dirty = true;
                }

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    items.RemoveAt(realIndex);
                    dirty = true;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // 操作按钮
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("生成枚举文件", GUILayout.Height(35)))
            {
                GenerateEnumFile(GetCurrentPath());
                dirty = false;
                EditorUtility.DisplayDialog("完成", $"枚举文件已生成，共 {items.Count} 项。", "确定");
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("从文件重新加载", GUILayout.Height(35)))
            {
                LoadFromFile(GetCurrentPath());
                dirty = false;
            }

            EditorGUILayout.EndHorizontal();

            if (dirty)
            {
                EditorGUILayout.HelpBox("有未保存的修改", MessageType.Warning);
            }
        }

        private void OnDisable()
        {
            if (dirty)
            {
                if (EditorUtility.DisplayDialog("未保存", "有修改未保存，要现在生成吗？", "生成", "放弃"))
                {
                    GenerateEnumFile(GetCurrentPath());
                }
            }
        }

        private string GetCurrentPath() => target == EditTarget.ModifyType ? MODIFY_ENUM_PATH : ACTION_ENUM_PATH;

        private void LoadFromFile(string path)
        {
            items.Clear();
            if (!File.Exists(path))
            {
                // 默认值
                if (target == EditTarget.ModifyType)
                {
                    items.Add("MoveSpeed");
                    items.Add("AttackSpeed");
                    items.Add("Attack");
                    items.Add("IncomingDamage");
                    items.Add("IncomingHeal");
                    items.Add("MaxHealth");
                    items.Add("Armor");
                }
                else
                {
                    items.Add("OnDamaged");
                    items.Add("OnAttackHit");
                    items.Add("OnKill");
                    items.Add("OnDeath");
                    items.Add("OnHealed");
                    items.Add("OnAttacked");
                }
                return;
            }

            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("//") || string.IsNullOrWhiteSpace(trimmed)) continue;
                if (trimmed.Contains("namespace") || trimmed.Contains("public enum") || trimmed == "{" || trimmed == "}") continue;

                var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"^\s*(\w+)\s*[=,]");
                if (match.Success)
                {
                    var name = match.Groups[1].Value;
                    if (name != "None") items.Add(name);
                }
            }
        }

        private void GenerateEnumFile(string path)
        {
            var enumName = target == EditTarget.ModifyType ? "BuffModifyType" : "BuffActionType";
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
            sb.AppendLine($"// 由 Buff 枚举编辑器生成");
            sb.AppendLine();
            sb.AppendLine("namespace TechCosmos.GBF.Runtime");
            sb.AppendLine("{");
            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine("    {");
            sb.AppendLine("        None = 0,");
            sb.AppendLine();

            for (int i = 0; i < items.Count; i++)
                sb.AppendLine($"        {items[i]} = {i + 1},");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            Debug.Log($"Buff 枚举文件已生成 -> {path}，共 {items.Count} 项");
        }

        private string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unnamed";
            name = name.Trim().Replace(" ", "_");
            if (!char.IsLetter(name[0]) && name[0] != '_')
                name = "_" + name;
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[^\w]", "");
            return name;
        }
    }
}
#endif