using System.Collections.Generic;
using UnityEngine;

namespace MyGame.UI
{
    /// <summary>
    /// 场景UI数据配置，用于存储特定场景应该加载哪些UI面板
    /// </summary>
    [CreateAssetMenu(fileName = "SceneUIData", menuName = "UI/Scene UI Data", order = 1)]
    public class SceneUIData : ScriptableObject
    {
        [Header("场景UI配置")]
        [Tooltip("场景名称")]
        public string sceneName;
        
        [Tooltip("此场景需要加载的UI面板ID列表")]
        public List<string> uiPanelIds = new List<string>();
    }
}