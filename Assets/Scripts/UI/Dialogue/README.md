# Dialogue 模块（可选）

对话模块是框架的**可选模块**：不进入核心存档/事件链，不用对话的品类完全不受影响。
模块只提供"怎么显示"，剧本内容与条件语义由游戏决定。

## 目录与职责

| 路径 | 职责 |
|---|---|
| `Data/DialogueData.cs` | `DialogueData`（ScriptableObject 剧本）、`DialogueNode`、`DialogueChoice`、`DialogueState` |
| `Core/DialogueManager.cs` | 运行时流程：开始/推进/选选项/结束、条件过滤、状态导出与恢复 |
| `Core/DialogueEvents.cs` | 模块内事件：Started / NodeChanged / Ended |
| `Core/IDialogueConditionProvider.cs` | 条件钩子：游戏实现 `IsConditionMet(key)` |
| `Model/Controller/View` | MVC 对话面板，打字机 + 选项 + 继续/跳过 |
| `Editor/DialoguePrefabBuilder.cs` | 一键烘焙面板与选项按钮预制体 |

## 接入步骤

1. **烘焙预制体**：菜单 `Tools/Game Jam Tool Pack/Rebuild Dialogue Prefabs`。
   生成 `Assets/Art Assets/UI/Dialogue/DialoguePanel.prefab` 与 `DialogueChoiceButton.prefab`。
2. **确认 UIConfig**：`Assets/Config/UI/UIConfig.asset` 中已有
   `panelType = DialoguePanel`、`addressableAddress = Assets/Art Assets/UI/Dialogue/DialoguePanel.prefab`、
   `lazyLoad = false`（启动即预加载，保证 DialoguePanelController 在任何对话开始前已订阅 DialogueEvents）。
3. **Addressables**：正式打包前把 `Assets/Art Assets/UI/Dialogue/` 加入 Addressables 组，
   Play Mode Script 使用 "Use Asset Database" 时按路径直接可用。
4. **开始对话**（玩法侧任意位置）：

```csharp
[SerializeField] private DialogueData dialogue;

DialogueManager.Instance.StartDialogue(dialogue, myConditionProvider);
```

面板会经 `DialogueEvents` 自动经 `UIManager` 显示，无需手动 Show/Hide。

交互约定：
- 无选项节点：点击对话面板任意位置（全屏蒙版）= 推进；打字中先补全当前句。
- 有选项节点：必须点击选项按钮，蒙版点击不会推进。
- 面板不再提供独立「继续」按钮（当前预制体已停用，重建后不再生成）。

## 条件（flag）怎么接

实现并注入提供者，框架不持有任何 flag：

```csharp
public sealed class MyDialogueFlags : IDialogueConditionProvider
{
    public bool IsConditionMet(string key) => key switch
    {
        "has_key" => inventory.HasKey(),
        "met_guard" => storyFlags.Contains("met_guard"),
        _ => false,
    };
}
```

`DialogueChoice.conditionKey` 留空表示无条件；无提供者时带条件的选项一律不显示。

## 存档怎么接（不碰 SaveData 核心）

对话状态是玩法数据的一部分，由 `IGameplaySaveProvider` 归口：

```csharp
// 保存
string stateJson = DialogueManager.Instance.ExportStateJson(); // 并入 gameplayDataJson

// 读档恢复：由你的 provider 解析出 DialogueState 后
DialogueState state = JsonUtility.FromJson<DialogueState>(stateJson);
DialogueData data = LoadDialogueAssetById(state.dialogueDataId); // 游戏自己解析资产
DialogueManager.Instance.RestoreState(state, data, myConditionProvider);
```

`DialogueState` 只保存 `dialogueDataId + currentNodeId`；对话变量/flag 由游戏在自己的存档结构里保存。

## 已知边界（第一版刻意不做）

- 无时间线演出、自动播放、语音轨、立绘系统、复杂 DSL；
- 打字机速度是面板上的全局参数，不做逐节点覆盖；
- 选项只做"可见即可选"，没有禁用态按钮；
- 所有选项被条件过滤且无 `nextNodeId` 兜底时，`Advance()` 会报错并拒绝推进，避免静默结束掩盖剧本配置错误。
