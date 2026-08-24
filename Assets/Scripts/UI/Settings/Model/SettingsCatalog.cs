using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.UI.Settings.Model
{
    /// <summary>
    /// 设置选项控件类型。每种类型对应一个行预制体，
    /// 新增选项只需在 SettingsCatalog 中添加定义，无需新增脚本或手工接线。
    /// </summary>
    public enum SettingsOptionType
    {
        Header = 0,
        Slider = 1,
        Toggle = 2,
        Dropdown = 3,
    }

    /// <summary>
    /// 单个设置选项的数据定义（与 UI 无关）。
    /// </summary>
    [Serializable]
    public class SettingsOptionDefinition
    {
        [Tooltip("选项键：SettingsPanelController.OptionRegistry 中注册的唯一 key")]
        public string key;

        [Tooltip("显示名称")]
        public string title;

        [Tooltip("控件类型")]
        public SettingsOptionType type;

        [Tooltip("Slider 最小值（真实值，非 0-1）")]
        public float minValue = 0f;

        [Tooltip("Slider 最大值（真实值，非 0-1）")]
        public float maxValue = 1f;

        [Tooltip("Slider 是否只取整数")]
        public bool wholeNumbers = false;

        [Tooltip("Dropdown 选项文本；key 为 resolutionIndex 时留空表示运行时生成分辨率列表")]
        public List<string> options = new();
    }

    /// <summary>
    /// 设置选项清单（数据表）。
    /// 由 SettingsRuntimeLayout 读取并按顺序生成设置面板行。
    /// 资产位于 Assets/Config/UI/SettingsCatalog.asset，并加入 Addressables
    /// Config 组（地址 Config/SettingsCatalog）；SettingsPage.prefab 同时保留直接引用。
    /// </summary>
    [CreateAssetMenu(fileName = "SettingsCatalog", menuName = "UI/Settings Catalog")]
    public class SettingsCatalog : ScriptableObject
    {
        [Header("按显示顺序排列；Header 类型作为分区标题")]
        [SerializeField] private List<SettingsOptionDefinition> m_entries = new();

        public List<SettingsOptionDefinition> Entries
        {
            get { return m_entries; }
        }

        public SettingsOptionDefinition GetEntry(string key)
        {
            foreach (SettingsOptionDefinition entry in m_entries)
            {
                if (entry != null && entry.key == key)
                {
                    return entry;
                }
            }
            return null;
        }
    }
}
