using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MyGame.DevTool
{
    /// <summary>
    /// 调试命令参数类型
    /// </summary>
    public enum DebugCommandParameterType
    {
        Int,
        Float,
        Bool,
        String,
    }

    /// <summary>
    /// 调试命令参数声明：名称、类型、是否可选。
    /// 用于帮助页展示与执行前的参数校验（个数 + 类型）。
    /// </summary>
    public class DebugCommandParameter
    {
        private static readonly Regex SpecRegex = new Regex(@"^(int|float|bool|string)(\?)?\s+(.+)$", RegexOptions.Compiled);

        /// <summary>参数名（仅用于错误提示与帮助展示）</summary>
        public string Name { get; }

        /// <summary>参数类型</summary>
        public DebugCommandParameterType Type { get; }

        /// <summary>是否为可选参数（可选参数只能出现在必选参数之后）</summary>
        public bool Optional { get; }

        public DebugCommandParameter(string name, DebugCommandParameterType type, bool optional = false)
        {
            Name = name;
            Type = type;
            Optional = optional;
        }

        /// <summary>
        /// 判断参数值能否转换为声明类型
        /// </summary>
        public bool IsValid(string value)
        {
            if (value == null)
            {
                return false;
            }

            switch (Type)
            {
                case DebugCommandParameterType.Int:
                    return int.TryParse(value, out _);
                case DebugCommandParameterType.Float:
                    return float.TryParse(value, out _);
                case DebugCommandParameterType.Bool:
                    return bool.TryParse(value, out _);
                case DebugCommandParameterType.String:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>类型的用户可读名称（用于错误提示）</summary>
        public string TypeName
        {
            get
            {
                switch (Type)
                {
                    case DebugCommandParameterType.Int: return "整数";
                    case DebugCommandParameterType.Float: return "数字";
                    case DebugCommandParameterType.Bool: return "布尔值";
                    default: return "文本";
                }
            }
        }

        /// <summary>
        /// 从特性 Params 字符串解析参数列表，格式："float 值 | string? 开关"。
        /// 解析失败返回 false 并给出错误说明。
        /// </summary>
        public static bool TryParseSpec(string spec, out DebugCommandParameter[] parameters, out string error)
        {
            parameters = null;
            error = null;

            if (string.IsNullOrWhiteSpace(spec))
            {
                parameters = Array.Empty<DebugCommandParameter>();
                return true;
            }

            var result = new List<DebugCommandParameter>();
            foreach (var part in spec.Split('|'))
            {
                string trimmed = part.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                var match = SpecRegex.Match(trimmed);
                if (!match.Success)
                {
                    error = $"无法解析参数片段: \"{trimmed}\"（应为 \"类型 名称\"，如 \"float 值\"）";
                    return false;
                }

                var type = match.Groups[1].Value switch
                {
                    "int" => DebugCommandParameterType.Int,
                    "float" => DebugCommandParameterType.Float,
                    "bool" => DebugCommandParameterType.Bool,
                    _ => DebugCommandParameterType.String,
                };
                bool optional = match.Groups[2].Success;
                result.Add(new DebugCommandParameter(match.Groups[3].Value, type, optional));
            }

            parameters = result.ToArray();
            return true;
        }
    }
}
