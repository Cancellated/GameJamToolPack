using System;

namespace MyGame.DevTool
{
    /// <summary>
    /// 调试命令特性，标记静态命令方法。
    /// 方法签名约定：CommandResult 方法名(DebugCommandContext ctx, string[] args)；
    /// 返回 void 亦可，注册表会将其视为 CommandResult.Ok()。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DebugCommandAttribute : Attribute
    {
        /// <summary>
        /// 命令名称（规范名，展示于帮助页）。命名约定：驼峰、小写开头，
        /// 多词命令如 timeScale / giveGold；单词命令保持小写（help/clear/ui）。
        /// 注册表索引统一转小写，输入匹配大小写不敏感（timeScale/TIMESCALE/timescale 均命中）。
        /// </summary>
        public string CommandName { get; }

        /// <summary>分类（帮助页按分类分组显示）</summary>
        public string Category { get; set; } = "其他";

        /// <summary>一句话描述</summary>
        public string Description { get; set; } = "";

        /// <summary>用法示例，如 "ui <面板> [on|off]"</summary>
        public string Usage { get; set; } = "";

        /// <summary>别名（可选）</summary>
        public string[] Aliases { get; set; }

        /// <summary>
        /// 参数签名，格式："float 值 | string? 开关"（类型[?] 名称，竖线分隔多个参数；
        /// 类型后带 ? 表示可选参数）。注册表按此签名在执行前做参数个数与类型校验。
        /// </summary>
        public string Params { get; set; } = "";

        /// <summary>
        /// 创建调试命令特性
        /// </summary>
        /// <param name="name">命令名称</param>
        public DebugCommandAttribute(string name)
        {
            CommandName = name;
        }
    }
}
