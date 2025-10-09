# 存档菜单操作流程时序图

根据存档菜单系统的MVC架构和实现代码，以下是完整的操作流程时序图：

```mermaid
sequenceDiagram
    participant User as 用户
    participant View as SaveLoadMenuView
    participant Controller as SaveLoadMenuController
    participant Model as SaveLoadMenuModel
    participant Events as GameEvents
    participant SaveManager as SaveManager
    participant UIManager as UIManager
    participant MainMenuView as MainMenuView
    participant MainMenuController as MainMenuController

    %% 初始化流程
    UIManager->>Controller: Show() - 打开存档菜单
    Controller->>Controller: InitializeSaveSlots() - 初始化存档槽数据
    Controller->>SaveManager: DoesSaveExist(slotName) - 检查存档是否存在
    SaveManager-->>Controller: 存档存在状态
    Controller->>SaveManager: LoadSaveData(slotName) - 加载存档数据
    SaveManager-->>Controller: 存档数据
    Controller->>Model: UpdateSaveSlots(slots) - 更新存档槽列表
    Model-->>Controller: OnSaveSlotsUpdated() - 存档槽更新事件
    Controller->>View: UpdateView() - 更新视图显示
    View->>View: CreateSaveSlotUIs() - 创建存档槽UI
    View->>View: HideSaveOptionsMenu() - 隐藏二级菜单

    %% 用户选择存档槽流程
    User->>View: 点击存档槽
    View->>Controller: HandleSaveSlotSelected(slotName, saveData) - 处理存档槽选中
    Controller->>Model: SetSelectedSaveSlot(slotName, saveData) - 设置选中存档槽
    Model-->>Controller: OnSelectedSaveSlotChanged() - 选中存档槽变更事件
    Controller->>View: UpdateView() - 更新视图
    View->>View: UpdateSaveOptionsButtonStates() - 更新按钮状态
    alt 非空存档槽
        View->>View: ShowSaveOptionsMenu() - 显示二级菜单
    else 空存档槽
        View->>View: HideSaveOptionsMenu() - 隐藏二级菜单
    end

    %% 保存游戏流程
    User->>View: 点击保存按钮
    View->>Controller: HandleSaveButtonClick() - 处理保存按钮点击
    Controller->>Events: TriggerSaveGame(slotName) - 触发保存游戏事件
    Events->>SaveManager: 执行保存操作
    SaveManager-->>Events: 保存完成事件
    Events->>Controller: HandleSaveGameCompleted(slotName, success) - 处理保存完成
    alt 保存成功
        Controller->>Controller: InitializeSaveSlots() - 刷新存档槽数据
        Controller->>Model: UpdateSaveSlots(slots) - 更新存档槽列表
        Model-->>Controller: OnSaveSlotsUpdated() - 存档槽更新事件
        Controller->>View: UpdateView() - 更新视图
        View->>View: HideSaveOptionsMenu() - 隐藏二级菜单
    end

    %% 加载游戏流程
    User->>View: 点击加载按钮
    View->>Controller: HandleLoadButtonClick() - 处理加载按钮点击
    Controller->>Events: TriggerLoadGame(slotName) - 触发加载游戏事件
    Events->>SaveManager: 执行加载操作
    SaveManager-->>Events: 加载完成事件
    Events->>Controller: HandleLoadGameCompleted(slotName, success) - 处理加载完成
    alt 加载成功
        Controller->>View: Hide() - 隐藏菜单
    end

    %% 删除存档流程
    User->>View: 点击删除按钮
    View->>Controller: HandleDeleteButtonClick() - 处理删除按钮点击
    Controller->>Events: TriggerDeleteSave(slotName) - 触发删除存档事件
    Events->>SaveManager: 执行删除操作
    SaveManager-->>Events: 删除完成事件
    Events->>Controller: HandleDeleteSaveCompleted(slotName, success) - 处理删除完成
    alt 删除成功
        Controller->>Controller: InitializeSaveSlots() - 刷新存档槽数据
        Controller->>Model: UpdateSaveSlots(slots) - 更新存档槽列表
        Model-->>Controller: OnSaveSlotsUpdated() - 存档槽更新事件
        Controller->>View: UpdateView() - 更新视图
        View->>View: HideSaveOptionsMenu() - 隐藏二级菜单
    end

    %% 取消操作流程
    User->>View: 点击取消按钮
    View->>View: HandleCancelButtonClick() - 处理取消按钮点击
    View->>View: HideSaveOptionsMenu() - 隐藏二级菜单

    %% 返回主菜单流程
    User->>View: 点击返回按钮
    View->>Controller: HandleBackButtonClick() - 处理返回按钮点击
    Controller->>Controller: HandleBackToMainMenu() - 处理返回主菜单
    Controller->>View: Hide() - 隐藏菜单
    
    %% 主菜单开始游戏自动创建存档流程
    User->>MainMenuView: 点击开始游戏按钮
    MainMenuView->>MainMenuController: OnStartGame() - 处理开始游戏
    MainMenuController->>Events: TriggerCreateNewGame() - 触发创建新游戏事件
    Events->>SaveManager: HandleCreateNewGame() - 处理创建新游戏
    SaveManager->>SaveManager: NewGame() - 创建新存档数据
    SaveManager->>SaveManager: SaveCurrentGame() - 自动保存到自动存档槽
    SaveManager-->>Events: 保存完成事件
    MainMenuController->>Events: TriggerGameStart() - 触发游戏开始事件
```

## 时序图说明

时序图展示了存档菜单系统的完整操作流程，包括以下几个主要阶段：

1. **初始化阶段**：UIManager打开存档菜单，控制器初始化存档槽数据，视图创建UI并显示存档列表。

2. **存档槽选择阶段**：用户点击存档槽，视图通知控制器，控制器更新模型中的选中状态，视图根据存档槽是否为空决定是否显示二级菜单。

3. **核心操作阶段**：
   - **保存游戏**：用户点击保存按钮，控制器触发游戏保存事件，SaveManager执行保存操作，完成后刷新UI。
   - **加载游戏**：用户点击加载按钮，控制器触发游戏加载事件，SaveManager执行加载操作，完成后隐藏菜单。
   - **删除存档**：用户点击删除按钮，控制器触发删除存档事件，SaveManager执行删除操作，完成后刷新UI。

4. **辅助操作阶段**：包括取消操作（隐藏二级菜单）和返回主菜单（隐藏整个存档菜单）。

5. **主菜单开始游戏自动创建存档流程**：
   - 用户点击主菜单的开始游戏按钮
   - MainMenuController先触发创建新游戏事件，自动保存新存档到自动存档槽
   - SaveManager处理创建新游戏事件，初始化存档数据并执行保存
   - 保存完成后，MainMenuController再触发游戏开始事件

整个流程采用了MVC架构和事件驱动设计，各组件间职责清晰，实现了解耦。视图负责UI显示和用户交互，控制器负责业务逻辑和事件转发，模型负责数据管理，而GameEvents则提供了跨系统通信机制。