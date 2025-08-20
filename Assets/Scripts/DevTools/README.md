# 🛠️ DevTools 模块文档

## 模块概述

DevTools模块提供了游戏开发过程中的调试工具，目前包含调试控制台功能，支持游戏内命令行调试和UI调试面板。

## 核心功能

### 🖥️ 调试控制台 (DebugConsole)

基于Unity UI的交互式调试控制台，提供以下功能：

- **命令行调试**：通过输入命令控制游戏状态
- **反射命令系统**：自动发现并注册标记了`[DebugCommand]`特性的方法
- **日志输出**：实时显示调试信息，支持最大100行日志缓存
- **UI调试面板**：可视化按钮界面，快速执行常用调试操作

## 输入映射

### 控制台快捷键
- **默认快捷键**：可在Unity Inspector中配置
- **切换显示**：通过`ToggleConsole()`方法控制显隐

### 命令列表

#### 游戏核心状态调试
| 命令 | 功能描述 |
|------|----------|
| `help` | 显示所有可用命令 |
| `restart` | 重新开始游戏 |
| `win` | 直接触发胜利 |
| `lose` | 直接触发失败 |

#### UI调试命令
| 命令 | 功能描述 |
|------|----------|
| `toggleallui` | 切换所有UI界面显隐 |
| `togglemainmenu` | 切换主菜单显隐 |
| `togglepausemenu` | 切换暂停菜单显隐 |
| `toggleresultpanel` | 切换结算面板显隐 |
| `togglehud` | 切换HUD显隐 |

## 使用示例

### 基本使用
```csharp
// 在场景中创建调试控制台
GameObject consoleObj = new GameObject("DebugConsole");
DebugConsole console = consoleObj.AddComponent<DebugConsole>();

// 设置UI引用
console.inputField = inputFieldComponent;
console.outputText = outputTextComponent;
```

### 自定义调试命令
```csharp
[DebugCommand("custom", Description = "自定义调试命令")]
private void CustomCommand()
{
    // 执行调试逻辑
    DebugConsole.Instance.Print("执行了自定义命令");
}
```

### 创建调试按钮
```csharp
// 添加调试按钮到指定容器
DebugConsole.Instance.AddButton("测试按钮", () => {
    // 按钮点击逻辑
    Debug.Log("按钮被点击");
});
```

## API参考

### DebugConsole类

#### 公共方法
```csharp
// 切换控制台显示状态
public void ToggleConsole()

// 处理命令输入
public void OnCommandEntered()

// 输出信息到控制台
void Print(string msg)

// 添加调试按钮
public void AddButton(string buttonName, Action onClick, Transform parent = null)
```

#### 特性定义
```csharp
[AttributeUsage(AttributeTargets.Method)]
public class DebugCommandAttribute : Attribute
{
    public string CommandName { get; }
    public string Description { get; set; }
    public DebugCommandAttribute(string name)
}
```

## 集成指南

### 1. 场景设置
1. 创建UI Canvas
2. 添加InputField用于命令输入
3. 添加Text用于输出显示
4. 将DebugConsole脚本挂载到GameObject
5. 在Inspector中关联UI组件

### 2. 预制件创建
推荐创建包含以下组件的预制件：
- Canvas
- InputField (命令输入)
- ScrollRect (日志显示)
- DebugConsole脚本

### 3. 运行时控制
```csharp
// 启用调试控制台
if (Input.GetKeyDown(KeyCode.F1))
{
    DebugConsole.Instance.ToggleConsole();
}
```

## 平台适配

- **PC平台**：支持键盘输入和鼠标点击
- **移动平台**：支持触屏输入（需额外配置虚拟键盘）
- **VR平台**：支持手柄输入（需适配VR UI系统）

## 性能优化

- **日志限制**：最大保留100行日志，防止内存泄漏
- **反射缓存**：在Awake阶段缓存所有命令方法，避免运行时反射开销
- **UI优化**：使用对象池管理调试按钮，避免频繁创建销毁

## 扩展功能

### 计划中的功能
- [ ] 命令参数解析支持
- [ ] 命令历史记录
- [ ] 自动补全功能
- [ ] 调试变量监视器
- [ ] 性能分析工具

### 自定义扩展
可以通过继承`DebugConsole`类来添加更多功能：
```csharp
public class CustomDebugConsole : DebugConsole
{
    [DebugCommand("level", Description = "跳转到指定关卡")]
    private void GotoLevelCommand(string levelName)
    {
        // 自定义关卡跳转逻辑
    }
}
```

## 相关文档

- [全局README](../README.md) - 项目总体架构
- [Control模块](../Control/README.md) - 输入控制系统
- [UI模块](../UI/README.md) - 用户界面系统
- [Managers模块](../Managers/README.md) - 游戏管理器

## 技术细节

### 命名空间
- `MyGame.DevTool` - 调试工具相关类

### 依赖关系
- `MyGame.Managers` - 游戏管理器
- `MyGame.Events` - 事件系统
- `UI.Managers` - UI管理系统
- Unity UI系统
- System.Reflection - 反射支持