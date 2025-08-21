/// <summary>
/// 日志的常量模块管理器
/// 为各个模块提供统一的日志标识符
/// </summary>
public static class LogModules
{
    // 系统模块
    public const string SYSTEM = "SYSTEM";
    public const string GAMEMANAGER = "GAMEMANAGER";
    
    // UI模块
    public const string UI = "UI";
    
    // 游戏数据模块
    public const string GAMEDATA = "GAMEDATA";
    public const string SAVE = "SAVE";
    
    // 调试模块
    public const string DEVTOOLS = "DEVTOOLS";
    
    // 游戏逻辑模块
    public const string PLAYER = "PLAYER";
    public const string AUDIO = "AUDIO";
    public const string SCENE = "SCENE";
    public const string INVENTORY = "INVENTORY";
}
