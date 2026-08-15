using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.UI.Config
{
    /// <summary>
    /// 面板输入模式：显示该面板时应切换到的输入状态。
    /// 由 UIConfig 配置驱动，替代 UIManager.SetUIState 中针对各面板的特判。
    /// </summary>
    public enum PanelInputMode
    {
        /// <summary>不切换输入模式（如 Loading / Console 等瞬态面板）</summary>
        None,

        /// <summary>UI 独占输入模式（如主菜单 / 暂停 / 结算 / 设置等）</summary>
        UI,

        /// <summary>游戏玩法输入模式（如 HUD，显示后玩家可操作游戏）</summary>
        Gameplay
    }

    /// <summary>
    /// UI 面板配置资产（面板的单一事实来源）。
    /// 定义每个面板类型的：Addressable 资源地址、懒加载标记、输入模式、互斥关系。
    ///
    /// 面板实例的获取优先级：
    ///   1. 场景中已放置的同类型面板（兼容现状，场景预置的面板直接注册）；
    ///   2. 配置清单中非懒加载条目：启动时经 PanelLoader 按 Addressable 地址加载到全局 UI 容器；
    ///   3. 懒加载条目：首次显示时经 PanelLoader 按 Addressable 地址加载（释放后再次显示会自动重载）。
    /// </summary>
    [CreateAssetMenu(fileName = "UIConfig", menuName = "UI/UIConfig")]
    public class UIConfig : ScriptableObject
    {
        /// <summary>
        /// 单个面板类型的配置条目
        /// </summary>
        [Serializable]
        public class PanelEntry
        {
            [Tooltip("面板类型")]
            public UIType panelType = UIType.None;

            [Tooltip("Addressable 资源地址（面板资源的唯一来源，运行时通过 Addressables 异步加载）")]
            public string addressableAddress;

            [Tooltip("懒加载：首次显示时才按需加载（适用于低频面板与场景专属面板）。" +
                "场景专属面板（如 MainMenu，随场景预置）必须标记懒加载——否则场景切换后" +
                "UIManager 补注册逻辑会在新场景中重建它，覆盖在其他界面之上")]
            public bool lazyLoad;

            [Tooltip("显示本面板时应切换的输入模式")]
            public PanelInputMode inputMode = PanelInputMode.None;

            [Tooltip("显示本面板时自动隐藏的其他面板类型（互斥关系）")]
            public UIType[] hideOnShow;
        }

        [Header("面板清单")]
        [SerializeField] private List<PanelEntry> _entries = new();

        /// <summary>
        /// 全部面板配置条目
        /// </summary>
        public List<PanelEntry> Entries
        {
            get { return _entries; }
        }

        /// <summary>
        /// 获取指定面板类型的配置条目；未配置时返回 null
        /// </summary>
        public PanelEntry GetEntry(UIType panelType)
        {
            foreach (var entry in _entries)
            {
                if (entry != null && entry.panelType == panelType)
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取指定面板的互斥名单（显示本面板时需隐藏的面板列表）；未配置返回空数组
        /// </summary>
        public UIType[] GetHideOnShow(UIType panelType)
        {
            var entry = GetEntry(panelType);
            return entry != null && entry.hideOnShow != null ? entry.hideOnShow : Array.Empty<UIType>();
        }

        /// <summary>
        /// 获取指定面板的输入模式；未配置返回 None（不切换输入）
        /// </summary>
        public PanelInputMode GetInputMode(UIType panelType)
        {
            var entry = GetEntry(panelType);
            return entry != null ? entry.inputMode : PanelInputMode.None;
        }

        /// <summary>
        /// 获取指定面板的 Addressable 资源地址（面板资源的唯一来源）；未配置返回 null
        /// </summary>
        public string GetAddressableAddress(UIType panelType)
        {
            var entry = GetEntry(panelType);
            return entry != null ? entry.addressableAddress : null;
        }

        /// <summary>
        /// 指定面板是否配置为懒加载（首次显示时才实例化）
        /// </summary>
        public bool IsLazyLoad(UIType panelType)
        {
            var entry = GetEntry(panelType);
            return entry != null && entry.lazyLoad;
        }
    }
}
