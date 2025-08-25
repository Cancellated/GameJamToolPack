/// <summary>
/// 日志的常量模块管理器
/// 为各个模块提供统一的日志标识符
/// </summary>
namespace Logger
{
    public static class LogModules
    {
        // 系统模块
        public const string SYSTEM = "[System]";
        public const string GAMEMANAGER = "[GameManager]";
        public const string MANAGERBOOTSTRAP = "[ManagerBootstrap]";

        // UI模块
        public const string UI = "[UI]";

        // 游戏数据模块
        public const string GAMEDATA = "[GameData]";
        public const string SAVE = "[Save]";

        // 调试模块
        public const string DEVTOOLS = "[DevTools]";

        // 游戏逻辑模块
        public const string PLAYER = "[Player]";
        public const string AUDIO = "[Audio]";
        public const string SCENE = "[Scene]";
        public const string INVENTORY = "[Inventory]";
    }
}