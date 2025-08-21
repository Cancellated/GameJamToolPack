# 🎮 Unity Game Jam 工具箱
针对gamejam中unity游戏开发的神奇妙妙工具，快速组装拼好游，目前为2D游戏开发框架:)

## 📦 预制件架构

### 系统数据流
![系统数据流图](Assets/Readme/diagrams/data-flow.png)

### 状态管理流程
![GameManager 状态机](Assets/Readme/diagrams/game-state-machine.png)

## 🛠 使用说明

### 核心预制件
| 预制件名称      | 功能描述                  | 依赖关系         |
|----------------|--------------------------|------------------|
| GameManager    | 全局状态管理              | 无              |
| AudioManager   | 音效播放控制              | GameManager     |
| UIManager      | UI 管理                  | GameManager     |
| PlayerController| 玩家控制器                | 无              |

### 示例代码
```csharp
// 状态切换
GameManager.Instance.ChangeState(GameState.Paused);
```

## 📂 模块文档

项目采用模块化设计，每个模块都有独立的文档：

| 模块名称 | 路径 | 描述 |
|----------|------|------|
| **Control** | `Assets/Scripts/Control/` | 玩家输入和游戏控制逻辑 |
| **DevTools** | `Assets/Scripts/DevTools/` | 调试工具和开发辅助功能 |
| **GameData** | `Assets/Scripts/GameData/` | 游戏数据持久化和存档管理 |
| **System** | `Assets/Scripts/System/` | 基础系统功能（事件系统、单例模式、日志系统） |

> 更多模块文档将逐步拆分整理...

### 操作控制
详细的控制说明请参考 [Control模块文档](Assets/Scripts/Control/README.md)

### 背包系统
| 按键       | 功能描述                  | 对应方法               |
|------------|--------------------------|-----------------------|
| 鼠标左键   | 选择物品                  | InventorySlot.OnSlotClick |
| 拖拽       | 移动物品                  | InventorySlot.OnBeginDrag/OnEndDrag |
| 右键       | 使用物品                  | InventoryController.UseItem |

![背包管理](Assets/Readme/diagrams/inventory.png)

![背包管理数据流](Assets/Readme/diagrams/inventory-mvc.png)

## 🔄 场景切换

### 场景管理流程
![场景切换流程](Assets/Readme/diagrams/scene-switch.png)

### 示例代码
```csharp
// 加载场景
SceneManager.LoadScene("Start");

// 监听场景加载完成事件
GameEvents.OnSceneChanged += (newScene) => {
    Debug.Log($"场景已切换至: {newScene.name}");
};
```

> 场景切换会触发GameManager的状态重置，请确保在场景切换前保存必要数据。

## 🖥 UI管理系统

### 核心功能
- **全局UI管理**：通过单例模式管理所有UI界面
- **状态管理**：定义7种UI状态(主菜单、暂停菜单等)并处理互斥关系
- **动画系统**：支持两种动画实现方式(协程淡入淡出和Animator状态机)
- **事件驱动**：通过GameEvents与其他模块通信
![UI管理流程](Assets/Readme/diagrams/ui-manager.png)

### 调试控制台
调试控制台功能已集成到 **DevTools模块**，支持游戏内命令行调试和可视化调试面板。

| 快捷键     | 功能描述                  |
|------------|--------------------------|
| ~          | 打开/关闭控制台           |
| Enter      | 执行命令                  |

详细功能说明请参考 [DevTools模块文档](Assets/Scripts/DevTools/README.md)

> 所有图表使用 Mermaid 设计，导出为 PNG 格式  
> 最后更新: {2025.6.26}