using System;

namespace MyGame.DevTool
{
    /// <summary>
    /// 调试命令元数据：名称/别名/分类/描述/用法/参数签名 + 执行器。
    /// Executor 接收 (执行上下文, 原始参数数组)；参数在进入 Executor 前
    /// 已由注册表按 Parameters 签名完成个数与类型校验。
    /// </summary>
    public class DebugCommandInfo
    {
        /// <summary>命令主名（注册时统一规范化为小写）</summary>
        public string Name { get; }

        /// <summary>别名（可为空，同样规范化索引）</summary>
        public string[] Aliases { get; }

        /// <summary>分类（帮助页分组依据）</summary>
        public string Category { get; }

        /// <summary>一句话描述</summary>
        public string Description { get; }

        /// <summary>用法示例，如 "ui <面板> [on|off]"</summary>
        public string Usage { get; }

        /// <summary>参数签名（用于校验与帮助展示，可为空数组）</summary>
        public DebugCommandParameter[] Parameters { get; }

        /// <summary>执行器：命令自解析参数并输出反馈，返回执行结果</summary>
        public Func<DebugCommandContext, string[], CommandResult> Executor { get; }

        public DebugCommandInfo(
            string name,
            string category,
            string description,
            string usage,
            DebugCommandParameter[] parameters,
            Func<DebugCommandContext, string[], CommandResult> executor,
            string[] aliases = null)
        {
            Name = name ?? "";
            Aliases = aliases ?? Array.Empty<string>();
            Category = string.IsNullOrWhiteSpace(category) ? "其他" : category;
            Description = description ?? "";
            Usage = usage ?? "";
            Parameters = parameters ?? Array.Empty<DebugCommandParameter>();
            Executor = executor;
        }
    }
}
