# 设置面板标准生成模式

设置界面由 **数据表 + 运行时生成器** 构成，行 UI 由 `SettingsRuntimeLayout` + `SettingsOptionRowView` 纯代码构建，不依赖行预制体、Sprite 或手工接线。

## 文件结构

| 文件 | 职责 |
|---|---|
| `Assets/Config/UI/SettingsCatalog.asset`（Addressable: `Config/SettingsCatalog`） | 设置项数据表（显示顺序、类型、范围、下拉选项） |
| `SettingsRuntimeLayout` | 纯代码构建 ScrollRect 内容区，读取 Catalog 逐行生成 |
| `SettingsOptionRowView` | 单行视图：Header / Toggle / Slider / Dropdown，经 `ISettingsValueProvider` 读写值 |
| `SettingsPanelController`（实现 `ISettingsValueProvider`） | key → 实际设置值 的 get/set 映射 |

## 如何新增一个设置项

1. 在 `SettingsCatalog.asset` 的 `m_entries` 中追加一条定义：
   - `key`：唯一标识；
   - `title`：显示名称；
   - `type`：`Header=0 / Slider=1 / Toggle=2 / Dropdown=3`；
   - Slider 需要 `minValue/maxValue/wholeNumbers`；
   - Dropdown 需要 `options`（`resolutionIndex` 留空会在运行时生成分辨率列表）。
2. 在 `SettingsPanelController.InitializeOptionRegistry()` 中为该 key 补一对 get/set。
3. 完成。若新 key 使用已有控件类型，无需新建预制体或脚本。

## 行为说明

- `SettingsRuntimeLayout.Build` 会创建居中单列的 ScrollRect 内容区，再按 Catalog 顺序生成；
- 行 UI 全部由代码创建：Header / Toggle / Slider / Dropdown 均无 Sprite 依赖；
- Catalog 通过 Inspector 直接引用或 Addressable 地址 `Config/SettingsCatalog` 加载；
- `SettingsCatalog.asset` 缺失时使用代码内置的默认选项表兜底；
- `developerMode` 是标准设置项：修改后标记未保存，点击“保存”后持久化生效；

## 已知问题与待办

以下为当前已知的次要问题，暂不阻塞功能，后续按需处理：

| 问题 | 说明 | 影响 | 处理方向 |
|---|---|---|---|
| 下拉弹窗只向下展开 | 底部行的下拉选项可能超出屏幕，无向上翻转逻辑 | 分辨率等选项较多的行在屏幕底部时体验差 | 增加屏幕空间判断，空间不足时向上展开 |
| 全量刷新 | 任一属性变化即刷新全部行（`RefreshAll`） | 当前约 10 行，开销可忽略 | 按 key 定向刷新（`RefreshRow(key)`） |
| `Show`/`Hide` 假定从主菜单进入 | 显示/隐藏时无条件恢复 `MainMenu` 交互 | 仅主菜单入口成立；若后续支持游戏内打开会出错 | 记录面板来源入口或改为通用交互管理 |
| 同步阻塞加载 Catalog | `ResolveCatalog` 使用 `WaitForCompletion()` | 首次实例化可能短暂卡帧，小资产可接受 | 改为异步 `await` 加载 |
