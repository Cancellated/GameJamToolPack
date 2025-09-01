using System.Collections.Generic;
using UnityEngine;

namespace MyGame.UI.Core
{
    /// <summary>
    /// 示例场景UI配置数据
    /// </summary>
    [CreateAssetMenu(fileName = "ExampleSceneUIData", menuName = "GameJam/UI/Scene UI Data/Example")]
    public class ExampleSceneUIData : ScriptableObject
    {
        [Header("场景信息")]
        [Tooltip("场景名称，必须与Unity中的场景名称完全一致")]
        public string sceneName = "GameScene";
        
        [Header("UI面板配置")]
        [Tooltip("该场景需要加载的UI面板ID列表")]
        public List<string> uiPanelIds = new List<string>
        {
            "HUD",
            "InventoryPanel",
            "Minimap"
        };
    }
}