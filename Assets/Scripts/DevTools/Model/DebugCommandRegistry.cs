using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Logger;
using MyGame.UI;

namespace MyGame.DevTool
{
    /// <summary>
    /// 调试命令注册表（模型层，命令的唯一事实源）。
    ///
    /// 职责：
    ///   1. 命令注册/注销/清空（含别名索引，名称统一规范化为小写）；
    ///   2. 反射扫描程序集中 [DebugCommand] 标记的静态命令方法（按命令名排序保证帮助页稳定）；
    ///   3. 输入解析（引号感知分词）→ 查找 → 参数校验 → 执行 → 异常捕获，
    ///      统一返回四态 CommandResult（Ok/NotFound/InvalidArgs/Error）。
    ///
    /// 业务系统可经 DebugConsoleController.Instance.Registry 动态 Register/Unregister 自己的命令。
    /// </summary>
    public class DebugCommandRegistry : BaseModel
    {
        private const string LOG_MODULE = LogModules.DEBUGCONSOLE;

        /// <summary>按注册顺序保存的命令列表（帮助页按此顺序输出）</summary>
        private readonly List<DebugCommandInfo> _commands = new();

        /// <summary>规范化名称/别名 → 命令 的索引</summary>
        private readonly Dictionary<string, DebugCommandInfo> _index = new();

        /// <summary>全部命令（只读，按注册顺序）</summary>
        public IReadOnlyList<DebugCommandInfo> Commands
        {
            get { return _commands; }
        }

        #region 生命周期

        public override void Initialize()
        {
            base.Initialize();
            if (IsInitialized)
            {
                ScanStaticCommands();
                Log.Info(LOG_MODULE, $"调试命令初始化完成，共加载 {_commands.Count} 个命令");
            }
        }

        public override void Cleanup()
        {
            if (IsInitialized)
            {
                Clear();
                base.Cleanup();
            }
        }

        #endregion

        #region 注册 / 注销

        /// <summary>
        /// 注册命令。主名已存在时替换旧命令并输出警告。
        /// </summary>
        public void Register(DebugCommandInfo command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.Name))
            {
                Log.Error(LOG_MODULE, "尝试注册空命令或命令名为空，已忽略");
                return;
            }

            string key = Normalize(command.Name);
            if (_index.TryGetValue(key, out var existing))
            {
                Log.Warning(LOG_MODULE, $"命令 \"{command.Name}\" 已存在，将替换旧命令");
                _commands.Remove(existing);
                foreach (var indexedKey in _index.Where(kvp => kvp.Value == existing).Select(kvp => kvp.Key).ToList())
                {
                    _index.Remove(indexedKey);
                }
            }

            _commands.Add(command);
            _index[key] = command;
            if (command.Aliases != null)
            {
                foreach (var alias in command.Aliases)
                {
                    if (!string.IsNullOrWhiteSpace(alias))
                    {
                        _index[Normalize(alias)] = command;
                    }
                }
            }
        }

        /// <summary>
        /// 注销指定名称的命令（主名或别名均可）
        /// </summary>
        public void Unregister(string name)
        {
            if (_index.TryGetValue(Normalize(name), out var command))
            {
                _commands.Remove(command);
                foreach (var indexedKey in _index.Where(kvp => kvp.Value == command).Select(kvp => kvp.Key).ToList())
                {
                    _index.Remove(indexedKey);
                }
            }
        }

        /// <summary>
        /// 清空全部命令（Cleanup 时调用；Console 重建后需重新扫描/注册）
        /// </summary>
        public void Clear()
        {
            _commands.Clear();
            _index.Clear();
        }

        /// <summary>
        /// 按名称（主名或别名）查找命令
        /// </summary>
        public bool TryFindCommand(string name, out DebugCommandInfo command)
        {
            return _index.TryGetValue(Normalize(name), out command);
        }

        #endregion

        #region 执行

        /// <summary>
        /// 执行整行输入：分词 → 查找 → 参数校验 → 执行。
        /// </summary>
        /// <param name="input">整行输入文本</param>
        /// <param name="context">执行上下文（由 Controller 创建注入）</param>
        public CommandResult Execute(string input, DebugCommandContext context)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return CommandResult.Ok();
            }

            var tokens = Tokenize(input);
            if (tokens == null)
            {
                // 引号未闭合：无法可靠划分命令与参数边界，直接拒绝执行
                return CommandResult.InvalidArgs("", "引号未闭合");
            }

            if (tokens.Count == 0)
            {
                return CommandResult.Ok();
            }

            if (!TryFindCommand(tokens[0], out var command))
            {
                return CommandResult.NotFound(tokens[0]);
            }

            var args = tokens.Skip(1).ToArray();
            if (!TryValidateArgs(command, args, out var error))
            {
                Log.Warning(LOG_MODULE, $"命令 {command.Name} 参数错误: {error}");
                return CommandResult.InvalidArgs(command.Usage, error);
            }

            try
            {
                return command.Executor(context, args) ?? CommandResult.Ok();
            }
            catch (Exception e)
            {
                Log.Error(LOG_MODULE, $"执行命令 {command.Name} 时出错: {e}");
                return CommandResult.Error(e.Message);
            }
        }

        #endregion

        #region 静态命令扫描

        /// <summary>
        /// 反射扫描程序集内所有 [DebugCommand] 标记的静态方法并注册。
        /// 方法签名要求：CommandResult Xxx(DebugCommandContext, string[]) 或返回 void。
        /// </summary>
        private void ScanStaticCommands()
        {
            Type[] types;
            try
            {
                types = typeof(DebugCommandRegistry).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
                Log.Warning(LOG_MODULE, "扫描命令时部分类型加载失败，已跳过: " + e.Message);
            }

            var candidates = new List<(DebugCommandAttribute Attr, MethodInfo Method)>();
            foreach (var type in types)
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attr = method.GetCustomAttribute<DebugCommandAttribute>();
                    if (attr == null)
                    {
                        continue;
                    }

                    if (!IsValidCommandSignature(method))
                    {
                        Log.Warning(LOG_MODULE,
                            $"跳过无效命令方法 {type.Name}.{method.Name}：签名应为 CommandResult Xxx(DebugCommandContext, string[])");
                        continue;
                    }

                    candidates.Add((attr, method));
                }
            }

            // 按命令名排序，保证帮助页输出顺序稳定
            foreach (var (attr, method) in candidates.OrderBy(c => c.Attr.CommandName))
            {
                DebugCommandParameter[] parameters = Array.Empty<DebugCommandParameter>();
                if (!string.IsNullOrWhiteSpace(attr.Params) &&
                    !DebugCommandParameter.TryParseSpec(attr.Params, out parameters, out var parseError))
                {
                    Log.Warning(LOG_MODULE, $"命令 \"{attr.CommandName}\" 的参数签名无效: {parseError}");
                    parameters = Array.Empty<DebugCommandParameter>();
                }

                var info = new DebugCommandInfo(
                    attr.CommandName,
                    attr.Category,
                    attr.Description,
                    attr.Usage,
                    parameters,
                    (ctx, args) => InvokeMethod(method, ctx, args),
                    attr.Aliases);
                Register(info);
            }
        }

        private static bool IsValidCommandSignature(MethodInfo method)
        {
            if (method.ReturnType != typeof(CommandResult) && method.ReturnType != typeof(void))
            {
                return false;
            }

            var ps = method.GetParameters();
            return ps.Length == 2 && ps[0].ParameterType == typeof(DebugCommandContext)
                && ps[1].ParameterType == typeof(string[]);
        }

        private static CommandResult InvokeMethod(MethodInfo method, DebugCommandContext context, string[] args)
        {
            try
            {
                object result = method.Invoke(null, new object[] { context, args });
                return result as CommandResult ?? CommandResult.Ok();
            }
            catch (TargetInvocationException e)
            {
                // 解包反射包装，把命令内部的真实异常抛给 Execute 统一处理
                throw e.InnerException ?? e;
            }
        }

        #endregion

        #region 解析辅助

        /// <summary>
        /// 命令名/别名规范化：统一小写并去除首尾空白。
        /// 命令规范名可为驼峰（如 timeScale），此处仅用于大小写不敏感的索引；
        /// 展示（帮助页）始终使用命令声明的规范名。
        /// </summary>
        private static string Normalize(string name)
        {
            return (name ?? "").ToLower().Trim();
        }

        /// <summary>
        /// 输入分词：按空白拆分，双引号内的内容（含空格）视为一个参数，引号本身被剥离。
        /// 引号未闭合时返回 null（由 Execute 返回参数错误，不静默吞掉剩余输入）。
        /// </summary>
        private static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            foreach (var ch in input)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(ch) && !inQuotes)
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }

            if (inQuotes)
            {
                return null;
            }

            if (sb.Length > 0)
            {
                tokens.Add(sb.ToString());
            }

            return tokens;
        }

        /// <summary>
        /// 按命令的参数签名校验参数个数与类型
        /// </summary>
        private static bool TryValidateArgs(DebugCommandInfo command, string[] args, out string error)
        {
            error = null;
            var parameters = command.Parameters;
            if (parameters == null || parameters.Length == 0)
            {
                if (args.Length > 0)
                {
                    error = "该命令不接受参数";
                    return false;
                }
                return true;
            }

            int requiredCount = parameters.Count(p => !p.Optional);
            if (args.Length < requiredCount)
            {
                error = $"缺少参数（至少需要 {requiredCount} 个）";
                return false;
            }

            if (args.Length > parameters.Length)
            {
                error = $"参数过多（最多 {parameters.Length} 个）";
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (!parameters[i].IsValid(args[i]))
                {
                    error = $"第 {i + 1} 个参数（{parameters[i].Name}）应为{parameters[i].TypeName}";
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
