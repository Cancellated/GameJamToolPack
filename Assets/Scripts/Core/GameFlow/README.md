# GameFlowCoordinator 使用说明

## 概述

GameFlowCoordinator 是一个专为管理游戏主要流程而设计的单例组件，它能够统一协调游戏中的关键操作，如开始新游戏、加载游戏和保存游戏等。通过使用这个组件，可以避免游戏事件的重复触发，确保游戏流程的一致性和可控性。

## 核心功能

- **统一流程控制**：提供单一入口点来触发游戏的主要流程
- **事件节流**：防止短时间内重复触发相同的游戏事件
- **生命周期管理**：在场景切换时保持组件的持久性
- **错误处理**：包含完善的日志记录和错误降级处理机制

## 如何使用

### 1. 添加组件到场景

将 GameFlowCoordinator 组件添加到主菜单场景中的一个GameObject上。由于它使用了单例模式和DontDestroyOnLoad，它会在整个游戏过程中保持存在。

### 2. 调用游戏流程方法

在需要触发游戏流程的地方，使用以下方式调用：

#### 开始新游戏

```csharp
// 使用默认存档槽（AutoSave）
if (GameFlowCoordinator.Instance != null)
{
    GameFlowCoordinator.Instance.StartNewGame();
}

// 使用指定存档槽
if (GameFlowCoordinator.Instance != null)
{
    GameFlowCoordinator.Instance.StartNewGame("save_1");
}
```

#### 加载游戏

```csharp
if (GameFlowCoordinator.Instance != null)
{
    GameFlowCoordinator.Instance.LoadGame("save_1");
}
```

#### 保存游戏

```csharp
if (GameFlowCoordinator.Instance != null)
{
    GameFlowCoordinator.Instance.SaveGame("save_1");
}
```

### 3. 替换原有事件触发代码

在你的项目中，找到所有直接触发游戏流程事件的代码，替换为使用 GameFlowCoordinator 的方式。例如：

**原代码：**
```csharp
GameEvents.TriggerCreateNewGame("save_1");
```

**新代码：**
```csharp
if (GameFlowCoordinator.Instance != null)
{
    GameFlowCoordinator.Instance.StartNewGame("save_1");
}
else
{
    // 降级处理：如果协调器不存在，仍然直接触发事件
    Log.Warning("MODULE_NAME", "GameFlowCoordinator实例不存在，直接触发CreateNewGame事件");
    GameEvents.TriggerCreateNewGame("save_1");
}
```

## 最佳实践

1. **统一使用协调器**：尽量在所有地方都使用 GameFlowCoordinator 来触发游戏流程，而不是直接使用 GameEvents

2. **检查实例存在性**：在调用协调器的方法前，始终检查实例是否存在，以避免空引用异常

3. **保留降级路径**：如果可能，保留直接使用 GameEvents 的降级路径，以提高代码的健壮性

4. **添加适当的日志**：在关键流程点添加日志，以便于调试和监控

5. **避免在Awake中调用**：由于协调器的Awake方法可能在其他组件之后执行，避免在Awake中立即调用协调器的方法

## 常见问题排查

### 协调器实例为空

如果 `GameFlowCoordinator.Instance` 始终返回 null，可能的原因有：

- 场景中没有添加 GameFlowCoordinator 组件
- 组件的 Awake 方法没有被正确执行
- 存在多个 GameFlowCoordinator 实例，导致多余的实例被销毁

### 事件仍然被重复触发

如果按照上述方法修改后，游戏事件仍然被重复触发，可能的原因有：

- 没有替换所有直接触发事件的代码
- 节流时间设置得太短，不足以防止重复触发
- 存在其他未通过协调器的事件触发路径

### 游戏流程异常

如果使用协调器后游戏流程出现异常，可能的原因有：

- 协调器的方法实现有问题
- 事件处理逻辑有错误
- 协调器的生命周期管理出现问题

## 扩展建议

根据项目的需要，你可以扩展 GameFlowCoordinator 的功能：

1. 添加更多游戏流程方法，如暂停游戏、恢复游戏、结束游戏等
2. 增强事件节流机制，添加更复杂的条件判断
3. 添加游戏流程的状态管理，跟踪当前游戏所处的流程阶段
4. 集成其他系统，如成就系统、统计系统等

