# 场景组件注册表系统

## 概述

场景组件注册表系统（SceneComponentRegistry）是一个用于管理Unity游戏中场景与UI组件绑定关系的灵活工具。通过该系统，开发者可以定义哪些场景应该加载哪些UI面板，并实现UI的自动加载和清理，无需在入口场景预设所有UI。

## 核心组件

### 1. SceneComponentRegistry

主要的管理器类，负责：
- 管理场景名称与UI面板ID的映射关系
- 监听场景加载/卸载事件
- 自动加载和清理场景UI
- 提供场景UI管理的API

### 2. SceneUIData

用于存储场景UI配置的ScriptableObject类，包含：
- 场景名称
- 该场景需要加载的UI面板ID列表

### 3. ExampleSceneUIData

示例配置文件，展示如何创建和配置场景UI数据。

### 4. SceneComponentRegistryInitializer

初始化器，确保系统在游戏启动时正确初始化。

### 5. UIManager扩展

扩展了原有的UIManager，添加了动态加载/卸载UI面板的功能：
- LoadUIPanel：从Resources加载UI面板
- UnloadUIPanel：卸载指定的UI面板
- UnloadAllUIPanels：卸载所有动态加载的UI面板

## 使用方法

### 1. 创建场景UI配置

1. 在Unity编辑器中，右键点击Project窗口
2. 选择 `Create > GameJam > UI > Scene UI Data > Example`
3. 设置场景名称和需要加载的UI面板ID列表

### 2. 准备UI预制体

1. 将UI预制体放置在Resources文件夹下的指定路径中（默认为`Resources/UI/Prefabs/`）
2. 确保预制体上挂载了实现IUIPanel接口的组件
3. 预制体的名称应与配置中的面板ID一致

### 3. 配置UIManager

1. 在场景中找到UIManager对象
2. 在Inspector面板中设置`UI Prefab Path`（UI预制体存放路径）
3. 确保UIManager正确初始化

### 4. 初始化系统

将SceneComponentRegistryInitializer脚本添加到入口场景中的任意GameObject上，它会自动：
- 创建并初始化SceneComponentRegistry实例
- 加载所有SceneUIData配置
- 设置事件监听

## 工作原理

1. **初始化阶段**：
   - SceneComponentRegistryInitializer创建SceneComponentRegistry实例
   - 系统加载所有SceneUIData配置并注册场景与UI面板的绑定关系

2. **场景加载阶段**：
   - 当场景加载完成时，系统触发OnSceneLoadComplete事件
   - SceneComponentRegistry响应事件，清理当前场景的UI
   - 根据新场景的注册信息，动态加载对应的UI面板
   - 面板加载后自动调用Initialize方法进行初始化

3. **场景卸载阶段**：
   - 当场景卸载时，系统触发OnSceneUnload事件
   - SceneComponentRegistry响应事件，清理当前场景的UI面板
   - 调用每个面板的Cleanup方法，然后卸载面板资源

## API参考

### SceneComponentRegistry

- `RegisterSceneUI(string sceneName, List<string> panelIds)`：注册场景与UI面板的绑定关系
- `UnregisterSceneUI(string sceneName)`：移除场景的UI面板注册
- `HasSceneUI(string sceneName)`：检查场景是否注册了UI面板
- `GetScenePanelIds(string sceneName)`：获取场景注册的UI面板ID列表
- `LoadSceneUIBindings()`：从配置加载场景UI绑定

### UIManager扩展

- `LoadUIPanel(string panelId)`：从Resources加载UI面板
- `UnloadUIPanel(string panelId)`：卸载指定的UI面板
- `UnloadAllUIPanels()`：卸载所有动态加载的UI面板

## 注意事项

1. 确保UI预制体路径正确，且预制体上挂载了实现IUIPanel接口的组件
2. 场景名称必须与Unity中的场景名称完全一致
3. UI面板ID应与预制体名称一致
4. 所有面板应正确实现Cleanup方法以释放资源
5. 在频繁切换场景时，注意监控内存使用情况

## 最佳实践

1. 为每个主要场景创建独立的SceneUIData配置文件
2. 将UI面板按功能分类存放，便于管理
3. 使用有意义的命名规范，避免面板ID冲突
4. 在面板的Cleanup方法中释放所有资源，避免内存泄漏
5. 定期检查和优化场景UI配置，移除不再使用的面板绑定

---

通过使用场景组件注册表系统，开发者可以更灵活地管理游戏中的UI，实现按需加载，优化内存使用，并提高场景切换的流畅度。