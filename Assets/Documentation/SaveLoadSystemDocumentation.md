# 游戏存档系统详细文档

## 1. 系统概述

存档系统是游戏中用于保存和加载玩家进度的核心功能模块。本系统采用MVC（Model-View-Controller）架构设计，支持自动存档和手动存档功能，并提供了完整的UI界面供玩家进行存档管理操作。

### 1.1 系统特点

- 支持自动存档和手动存档两种模式
- 采用Addressable Assets系统加载UI资源，提高性能
- 完整的存档数据管理，包括游戏进度、关卡信息等
- 友好的用户界面，支持存档槽选择、保存、加载和删除操作
- 可配置的存档参数，如存档数量限制、自动存档间隔等
- 视觉和音效反馈系统

## 2. 系统架构与目录结构

存档系统采用MVC架构模式，各模块职责明确，耦合度低。

### 2.1 目录结构

```
Assets/
├── Scripts/
│   └── UI/
│       └── SaveLoadMenu/
│           ├── Controller/
│           │   └── SaveLoadMenuController.cs
│           ├── Model/
│           │   ├── SaveLoadMenuConfig.cs
│           │   └── SaveLoadMenuModel.cs
│           └── View/
│               ├── SaveLoadMenuPanel.cs
│               └── SaveLoadMenuView.cs
├── Config/
│   └── SaveLoadMenuConfig.asset
├── AddressableAssetsData/
│   └── [Addressable配置文件]
└── Art Asset/
    └── Prefabs/
        └── [存档槽预制件]
```

### 2.2 核心模块说明

| 模块 | 主要职责 | 文件位置 | 引用 |
|------|----------|----------|------|
| 模型层 | 管理存档数据和状态 | Scripts/UI/SaveLoadMenu/Model/ | <mcfile name="SaveLoadMenuModel.cs" path="d:\unity\Unity_Projects\Game Jam Tool Pack\Assets\Scripts\UI\SaveLoadMenu\Model\SaveLoadMenuModel.cs"></mcfile> |
| 视图层 | 处理UI显示和用户交互 | Scripts/UI/SaveLoadMenu/View/ | <mcfile name="SaveLoadMenuView.cs" path="d:\unity\Unity_Projects\Game Jam Tool Pack\Assets\Scripts\UI\SaveLoadMenu\View\SaveLoadMenuView.cs"></mcfile> |
| 控制器层 | 协调模型和视图的通信 | Scripts/UI/SaveLoadMenu/Controller/ | <mcfile name="SaveLoadMenuController.cs" path="d:\unity\Unity_Projects\Game Jam Tool Pack\Assets\Scripts\UI\SaveLoadMenu\Controller\SaveLoadMenuController.cs"></mcfile> |
| 配置文件 | 存储系统配置参数 | Config/ | <mcfile name="SaveLoadMenuConfig.asset" path="d:\unity\Unity_Projects\Game Jam Tool Pack\Assets\Config\SaveLoadMenuConfig.asset"></mcfile> |

## 3. 核心功能详解

### 3.1 存档数据管理

存档系统的核心是对存档数据的管理，包括创建、读取、更新和删除存档。

#### 3.1.1 存档数据结构

```csharp
// 存档数据结构 (SaveData)
public class SaveData
{
    public string saveTime; // 存档时间
    public string version; // 游戏版本
    public GameProgress gameProgress; // 游戏进度数据
    // 其他需要保存的数据
}

// 存档槽信息结构
public struct SaveSlotInfo
{
    public string SlotName; // 存档槽名称
    public string DisplayName; // 显示名称
    public bool HasSave; // 是否有存档
    public string LastModifiedTime; // 最后修改时间
    public string Version; // 游戏版本
    public string ProgressText; // 进度文本
    public SaveData SaveData; // 存档数据
    public bool IsAutoSaveSlot; // 是否为自动存档槽
}
```

#### 3.1.2 存档槽管理

- 自动存档槽：系统定期自动创建的存档，数量受配置限制
- 手动存档槽：玩家手动创建的存档，数量受配置限制
- 存档槽信息存储在`SaveLoadMenuModel`中，通过`SaveSlots`属性访问

### 3.2 配置系统

存档系统提供了丰富的配置选项，方便开发者根据游戏需求进行调整。

#### 3.2.1 配置项详解

| 配置类别 | 配置项 | 说明 | 默认值 |
|---------|-------|------|-------|
| 存档设置 | AutoSaveEnabled | 是否启用自动存档 | true |
| 存档设置 | AutoSaveIntervalMinutes | 自动存档间隔（分钟） | 5 |
| 存档设置 | MaxAutoSaveCount | 最大自动存档数量 | 3 |
| 存档设置 | MaxManualSaveCount | 最大手动存档数量 | 6 |
| UI设置 | SaveSlotPrefabAddress | 存档槽预制件的Addressable地址 | "UI/SaveSlot" |
| UI设置 | MenuAnimationDuration | 菜单动画持续时间 | 0.3f |
| UI设置 | SlotAnimationDuration | 槽位动画持续时间 | 0.2f |
| 文本设置 | NewGameConfirmationText | 创建新游戏的确认文本 | "创建新游戏将覆盖当前进度，确定要继续吗？" |
| 文本设置 | DeleteSaveConfirmationText | 删除存档的确认文本 | "确定要删除此存档吗？此操作不可恢复。" |
| 文本设置 | LoadSaveConfirmationText | 加载存档的确认文本 | "加载此存档将覆盖当前进度，确定要继续吗？" |
| 视觉反馈 | SelectedSlotColor | 选中存档槽的颜色 | Color.yellow |
| 视觉反馈 | AutoSaveSlotColor | 自动存档槽的颜色 | Color.blue |
| 视觉反馈 | ManualSaveSlotColor | 手动存档槽的颜色 | Color.green |
| 视觉反馈 | EmptySlotColor | 空存档槽的颜色 | Color.gray |

#### 3.2.2 配置文件使用

配置文件`SaveLoadMenuConfig.asset`位于`Assets/Config/`目录下，可在Unity编辑器中直接修改配置参数。系统启动时，控制器会自动加载此配置文件。

```csharp
// 配置文件的定义
[CreateAssetMenu(fileName = "SaveLoadMenuConfig", menuName = "GameJamToolPack/SaveLoadMenuConfig")]
public class SaveLoadMenuConfig : ScriptableObject
{
    // 存档设置
    [Header("存档设置")]
    public bool AutoSaveEnabled = true;
    public int AutoSaveIntervalMinutes = 5;
    public int MaxAutoSaveCount = 3;
    public int MaxManualSaveCount = 6;
    
    // UI设置
    [Header("UI设置")]
    public string SaveSlotPrefabAddress = "UI/SaveSlot";
    public float MenuAnimationDuration = 0.3f;
    public float SlotAnimationDuration = 0.2f;
    
    // 文本设置
    [Header("文本设置")]
    public string NewGameConfirmationText = "创建新游戏将覆盖当前进度，确定要继续吗？";
    public string DeleteSaveConfirmationText = "确定要删除此存档吗？此操作不可恢复。";
    public string LoadSaveConfirmationText = "加载此存档将覆盖当前进度，确定要继续吗？";
    
    // 视觉反馈设置
    [Header("视觉反馈设置")]
    public Color SelectedSlotColor = Color.yellow;
    public Color AutoSaveSlotColor = Color.blue;
    public Color ManualSaveSlotColor = Color.green;
    public Color EmptySlotColor = Color.gray;
}
```

### 3.3 用户界面系统

存档系统提供了完整的用户界面，包括存档槽显示、操作按钮和信息反馈。

#### 3.3.1 UI组件结构

- **主面板**: 包含所有UI元素，由`SaveLoadMenuPanel`控制
- **存档槽容器**: 容纳所有存档槽UI，自动根据存档数量生成
- **存档槽UI**: 显示单个存档的信息，包括名称、时间、进度等
- **功能按钮**: 包括保存、加载、删除、新游戏和返回按钮
- **存档选项菜单**: 显示对选中存档的操作选项

#### 3.3.2 UI交互流程

1. 用户打开存档菜单，系统加载并显示所有存档槽
2. 用户点击存档槽，选中该槽位并显示存档信息
3. 根据存档槽是否为空，系统提供不同的操作选项：
   - 空槽位：直接提供保存选项
   - 有存档槽位：提供保存、加载、删除选项
4. 用户选择操作后，系统执行相应的存档操作并提供反馈

### 3.4 事件系统

存档系统使用事件系统实现模块间的通信，主要事件包括：

| 事件名称 | 触发条件 | 参数 |
|---------|---------|------|
| SaveSlotSelected | 存档槽被选中 | slotName, saveData |
| SaveGame | 保存游戏 | slotName |
| LoadGame | 加载游戏 | slotName |
| DeleteSave | 删除存档 | slotName |
| CreateNewGame | 创建新游戏 | 无 |
| BackToMainMenu | 返回主菜单 | 无 |
| SaveSlotsUpdated | 存档槽列表更新 | 无 |
| SelectedSaveSlotChanged | 选中的存档槽变更 | 无 |

## 4. 技术实现细节

### 4.1 MVC架构实现

存档系统严格遵循MVC架构，各层职责明确：

- **模型层 (Model)**: `SaveLoadMenuModel`负责管理存档数据和状态
- **视图层 (View)**: `SaveLoadMenuView`和`SaveLoadMenuPanel`负责UI显示和用户交互
- **控制器层 (Controller)**: `SaveLoadMenuController`负责协调模型和视图之间的通信

```csharp
// 控制器初始化示例
protected override void OnInitialize()
{
    base.OnInitialize();
    
    // 初始化MVC组件
    InitializeMVC();
    
    // 注册事件
    RegisterEvents();
    
    // 初始化存档槽
    InitializeSaveSlots();
}
```

### 4.2 Addressable Assets集成

存档系统使用Unity的Addressable Assets系统来加载UI预制件，提高资源加载性能和管理效率。

```csharp
// 使用Addressable加载存档槽预制件
protected virtual async void CreateSaveSlotUIs()
{
    // 异步加载存档槽预制件
    AsyncOperationHandle<GameObject> prefabLoadHandle = Addressables.LoadAssetAsync<GameObject>(config.SaveSlotPrefabAddress);
    _prefabLoadHandles.Add(prefabLoadHandle);
    
    // 等待加载完成
    await prefabLoadHandle.Task;
    
    if (prefabLoadHandle.Status == AsyncOperationStatus.Succeeded && prefabLoadHandle.Result != null)
    {
        GameObject saveSlotPrefab = prefabLoadHandle.Result;
        
        // 创建存档槽UI
        foreach (var slotInfo in _model.SaveSlots)
        {
            GameObject slotGO = Instantiate(saveSlotPrefab, saveSlotsContainer);
            // 初始化存档槽UI
        }
    }
}
```

### 4.3 异步操作处理

存档系统使用异步操作处理耗时的任务，如资源加载和存档读写，避免阻塞主线程。

- 存档槽预制件的加载采用`async/await`模式
- 存档数据的读写操作也应采用异步方式实现（可根据具体存储方案扩展）

### 4.4 资源管理

系统实现了完善的资源管理机制，确保不会发生内存泄漏：

- 清理Addressable加载句柄
- 销毁不再使用的游戏对象
- 取消事件订阅

```csharp
// 资源清理示例
protected virtual void ClearSaveSlotUIs()
{
    foreach (var slotUI in _saveSlotUIs)
    {
        if (slotUI is not null and MonoBehaviour)
        {
            Destroy(((MonoBehaviour)slotUI).gameObject);
        }
    }
    
    _saveSlotUIs.Clear();
}
```

## 5. 使用指南与开发流程

### 5.1 在Unity编辑器中设置存档系统

1. **创建配置文件**:
   - 在Unity编辑器中，右键点击`Assets/Config/`目录
   - 选择`Create > GameJamToolPack > SaveLoadMenuConfig`
   - 根据游戏需求调整配置参数

2. **创建UI面板**:
   - 在场景中创建一个Canvas对象
   - 添加一个面板作为存档菜单的根对象
   - 为面板添加`SaveLoadMenuPanel`脚本
   - 根据UI组件要求添加所有必要的UI元素
   - 在Inspector中绑定组件引用

3. **设置控制器**:
   - 在场景中添加一个空对象作为控制器
   - 为其添加`SaveLoadMenuController`脚本
   - 在Inspector中配置`Model`、`View`和`Config`属性

4. **配置Addressable Assets**:
   - 确保存档槽预制件已添加到Addressable Assets中
   - 设置正确的Addressable地址，与配置文件中的`SaveSlotPrefabAddress`匹配

### 5.2 扩展存档系统

开发者可以根据游戏需求扩展存档系统的功能：

1. **添加新的存档数据**:
   - 修改`SaveData`类，添加需要保存的新数据字段
   - 更新存档和加载逻辑，处理新添加的数据

2. **自定义UI样式**:
   - 修改存档槽预制件的外观
   - 自定义菜单动画和过渡效果
   - 添加新的UI反馈元素

3. **实现新的存储方案**:
   - 目前系统框架支持扩展不同的存储方案
   - 可以实现本地文件存储、云端存储等不同的存储方式

### 5.3 最佳实践

- 定期自动存档，避免玩家进度丢失
- 限制自动存档数量，避免占用过多存储空间
- 提供清晰的存档信息反馈，包括时间、进度等
- 执行关键操作前提供确认提示，如删除存档、覆盖进度等
- 确保UI响应迅速，操作流程直观

## 6. 故障排除与常见问题

### 6.1 存档槽预制件加载失败

**问题描述**: 系统无法加载存档槽预制件，出现错误提示。

**可能原因**: 
- Addressable地址配置错误
- 预制件未正确添加到Addressable Assets中
- Addressable资源未构建

**解决方案**: 
- 检查`SaveLoadMenuConfig`中的`SaveSlotPrefabAddress`配置是否正确
- 确认存档槽预制件已添加到Addressable Assets中
- 执行Addressable资源构建操作

### 6.2 存档操作无响应

**问题描述**: 点击保存/加载/删除按钮后，系统没有响应。

**可能原因**: 
- 按钮事件未正确绑定
- 控制器未正确引用模型或视图
- 存档数据读写失败

**解决方案**: 
- 检查按钮事件绑定是否正确
- 确认控制器的`Model`、`View`和`Config`属性已正确配置
- 检查存档数据读写逻辑是否存在问题

### 6.3 存档数据丢失

**问题描述**: 游戏重启后，之前的存档数据丢失。

**可能原因**: 
- 存档文件未正确保存
- 存档路径设置错误
- 存储介质访问权限问题

**解决方案**: 
- 检查存档文件的保存路径和权限设置
- 验证存档数据的序列化和反序列化逻辑
- 添加日志记录，追踪存档操作过程

## 7. 版本更新日志

| 版本 | 更新内容 | 日期 |
|------|---------|------|
| 1.0 | 初始版本，实现基本的存档/读档功能 | 2023-10-15 |
| 1.1 | 添加自动存档功能和视觉反馈系统 | 2023-10-20 |
| 1.2 | 集成Addressable Assets系统，优化资源加载 | 2023-10-25 |
| 1.3 | 完善配置系统，支持更多自定义选项 | 2023-11-01 |

## 8. 附录

### 8.1 代码优化建议

1. **存档数据加密**:
   - 为保护存档数据不被篡改，可以添加数据加密功能
   - 实现简单的加密算法或使用Unity的加密API

2. **存档压缩**:
   - 对于大型存档文件，可以添加压缩功能，减少存储空间占用
   - 使用C#的压缩库如`System.IO.Compression`

3. **云同步支持**:
   - 集成云存储服务，实现跨设备存档同步
   - 考虑使用Unity Cloud Save或其他第三方云存储服务

4. **自动备份**:
   - 为重要存档创建自动备份，防止意外删除或损坏
   - 实现版本控制功能，允许玩家回滚到之前的存档状态