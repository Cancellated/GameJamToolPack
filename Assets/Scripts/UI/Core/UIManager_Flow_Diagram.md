# UIManager UI显隐控制链流程图

## 概述
本文档详细分析了`UIManager`类中的UI显隐控制流程，包括初始化、事件响应、UI状态管理以及界面互斥关系的处理机制。

## 初始化流程

```mermaid
flowchart TD
    A[UIManager.Awake] --> B[InitializePanelMap]
    B --> C[遍历uiPanels列表]
    C --> D{面板不为空且不存在于映射中?}
    D -->|是| E[添加到_panelMap字典]
    E --> F[调用panel.Initialize]
    D -->|否| C
    F --> G[HideAllUI]
    G --> H[隐藏所有非控制台UI]
    H --> I[设置currentState为None]
    I --> J[注册事件监听器]
    J --> K[GameEvents.OnMenuShow += OnMenuShow]
    K --> L[GameEvents.OnSceneLoadStart += ShowLoading]
    L --> M[GameEvents.OnSceneLoadComplete += HideLoading]
```

## UI显隐核心控制流程

```mermaid
flowchart TD
    subgraph 事件触发层
        A1[外部模块触发GameEvents.OnMenuShow]
        A2[场景加载系统触发OnSceneLoadStart]
        A3[场景加载系统触发OnSceneLoadComplete]
    end

    subgraph 事件响应层
        B1[OnMenuShow处理UI显隐]
        B2[ShowLoading处理加载界面显示]
        B3[HideLoading处理加载界面隐藏]
    end

    subgraph 核心处理层
        C[SetUIState 状态设置与互斥处理]
        D{显示UI?}
        E{隐藏UI且为最后一个非特殊UI?}
        F[更新currentState]
        G[执行实际的Show/Hide操作]
    end

    subgraph 输入模式控制
        H[InputManager.SwitchToUIMode]
        I[InputManager.SwitchToGamePlayMode]
    end

    subgraph UI互斥关系处理
        J[处理UI互斥逻辑]
    end

    A1 --> B1
    A2 --> B2
    A3 --> B3
    
    B1 --> C
    B2 --> C
    B3 --> C
    
    C --> D
    D -->|是| H
    D -->|是| J
    D -->|否| E
    E -->|是| I
    
    J --> F
    H --> F
    I --> F
    
    F --> G
    G --> K[调用panel.Show或panel.Hide]
```

## UI互斥关系图

```mermaid
flowchart TD
    subgraph 互斥组1
        MM[MainMenu]
        PM[PauseMenu]
        HUD[HUD]
        MM ---|互斥| PM
        MM ---|互斥| HUD
        PM ---|互斥| MM
    end

    subgraph 互斥组2
        PM2[PauseMenu]
        RP[ResultPanel]
        PM2 ---|互斥| RP
        RP ---|互斥| PM2
        RP ---|互斥| HUD2[HUD]
    end

    subgraph 互斥组3
        INV[Inventory]
        MM2[MainMenu]
        PM3[PauseMenu]
        INV ---|互斥| MM2
        INV ---|互斥| PM3
    end

    subgraph 互斥组4
        SP[SettingsPanel]
        AP[AboutPanel]
        MM3[MainMenu]
        PM4[PauseMenu]
        RP2[ResultPanel]
        SP ---|互斥| MM3
        SP ---|互斥| PM4
        SP ---|互斥| RP2
        AP ---|互斥| MM3
        AP ---|互斥| PM4
        AP ---|互斥| RP2
    end

    subgraph 特殊界面
        LOAD[Loading]
        CONSOLE[Console]
        LOAD -.->|不与任何UI互斥| END
        CONSOLE -.->|不与任何UI互斥| END
    end
```

## 加载界面显隐流程

```mermaid
sequenceDiagram
    participant 场景加载系统
    participant UIManager
    participant LoadingScreen
    participant IUIPanel

    %% 显示加载界面流程
    场景加载系统->>UIManager: 触发OnSceneLoadStart事件
    UIManager->>UIManager: ShowLoading(sceneName)
    UIManager->>UIManager: SetUIState(UIType.Loading, true)
    UIManager->>IUIPanel: 调用panel.Show()
    UIManager->>LoadingScreen: 类型转换为LoadingScreen
    UIManager->>LoadingScreen: 调用OnSceneLoadStarted(sceneName)

    %% 隐藏加载界面流程
    场景加载系统->>UIManager: 触发OnSceneLoadComplete事件
    UIManager->>LoadingScreen: 调用OnSceneLoadCompleted(sceneName)
    UIManager->>UIManager: SetUIState(UIType.Loading, false)
    UIManager->>IUIPanel: 调用panel.Hide()
```

## 关键控制方法说明

### SetUIState 方法流程

```mermaid
flowchart TD
    A[SetUIState(UIType state, bool show)] --> B{show为true?}
    B -->|是| C{InputManager实例存在?}
    C -->|是| D{state不是Console或Loading?}
    D -->|是| E[InputManager.SwitchToUIMode]
    
    B -->|是| F[处理UI互斥关系]
    F --> G[根据state类型调用SetUIState隐藏互斥UI]
    
    B -->|否| H{currentState == state且非特殊UI?}
    H -->|是| I{InputManager实例存在?}
    I -->|是| J[InputManager.SwitchToGamePlayMode]
    
    G --> K[更新currentState]
    E --> K
    J --> K
    
    K --> L{_panelMap中存在该state?}
    L -->|是| M{show为true?}
    M -->|是| N[panel.Show()]
    M -->|否| O[panel.Hide()]
    L -->|否| P[结束]
```

## 代码优化建议

1. **错误处理增强**：在尝试类型转换和调用面板方法时添加更健壮的错误处理，避免空引用异常。

2. **UI层级管理**：考虑添加UI层级管理机制，确保UI显示在正确的层级上。

3. **异步加载支持**：为复杂UI面板添加异步加载支持，提高性能和用户体验。

4. **事件参数封装**：将UI显隐事件的参数封装为专用的事件数据类，提高代码可维护性。

5. **状态验证**：添加状态验证逻辑，防止非法状态切换。

## 输入输出示例

#### 输入输出示例
输入：
```csharp
// 外部系统触发主菜单显示
GameEvents.OnMenuShow?.Invoke(UIType.MainMenu, true);
```

输出：
```
1. UIManager.OnMenuShow 被调用
2. SetUIState(UIType.MainMenu, true) 被调用
3. 检查InputManager并切换到UIMode
4. 隐藏互斥UI (PauseMenu, HUD)
5. 更新currentState为MainMenu
6. 调用MainMenu面板的Show()方法
```

通过以上流程图和说明，可以清晰地了解`UIManager`中UI显隐控制的完整链路和机制。