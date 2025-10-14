using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Data;
using MyGame.UI;
using MyGame.Managers;

namespace MyGame.UI.SaveLoad
{
    /// <summary>
    /// 存档菜单的数据模型类，继承自ObservableModel
    /// 用于管理和提供存档菜单所需的数据
    /// </summary>
    public class SaveLoadMenuModel : ObservableModel
    {
        [Header("配置文件")]
        [Tooltip("存档菜单配置文件，包含存档设置、UI配置等")]
        [SerializeField] private SaveLoadMenuConfig _config;
        
        private SaveData _selectedSaveData;
        private string _selectedSaveSlotName;
        private bool _isAutoSaveSlot;
        private List<SaveSlotInfo> _saveSlots = new();
        
        /// <summary>
        /// 存档菜单配置文件
        /// </summary>
        public SaveLoadMenuConfig Config
        {
            get { return _config; }
            set { _config = value; }
        }
        
        /// <summary>
        /// 存档槽信息更新事件
        /// </summary>
        public event System.Action OnSaveSlotsUpdated;
        
        /// <summary>
        /// 选中存档槽变更事件
        /// </summary>
        public event System.Action OnSelectedSaveSlotChanged;
        
        /// <summary>
        /// 选中的存档数据
        /// </summary>
        public SaveData SelectedSaveData
        {
            get { return _selectedSaveData; }
            set 
            {
                SetProperty(ref _selectedSaveData, value, nameof(SelectedSaveData));
                OnSelectedSaveSlotChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// 选中的存档槽名称
        /// </summary>
        public string SelectedSaveSlotName
        {
            get { return _selectedSaveSlotName; }
            set 
            {
                SetProperty(ref _selectedSaveSlotName, value, nameof(SelectedSaveSlotName));
                OnSelectedSaveSlotChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// 是否为自动存档槽
        /// </summary>
        public bool IsAutoSaveSlot
        {
            get { return _isAutoSaveSlot; }
            set { SetProperty(ref _isAutoSaveSlot, value, nameof(IsAutoSaveSlot)); }
        }
        
        /// <summary>
        /// 存档槽信息列表
        /// </summary>
        public List<SaveSlotInfo> SaveSlots
        {
            get { return _saveSlots; }
            set 
            {
                SetProperty(ref _saveSlots, value, nameof(SaveSlots));
                OnSaveSlotsUpdated?.Invoke();
            }
        }
        
        /// <summary>
        /// 初始化模型
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            // 初始化数据
            _saveSlots = new List<SaveSlotInfo>();
        }
        
        /// <summary>
        /// 清理模型资源
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
            // 清理资源
            _saveSlots.Clear();
            _selectedSaveData = null;
            _selectedSaveSlotName = null;
        }
        
        /// <summary>
        /// 设置选中的存档槽
        /// 优化：避免多次触发OnSelectedSaveSlotChanged事件
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        /// <param name="saveData">存档数据</param>
        public void SetSelectedSaveSlot(string slotName, SaveData saveData = null)
        {
            bool slotChanged = false;
            bool dataChanged = false;
            bool autoSaveChanged = false;
            
            // 如果有存档数据，优先使用存档数据中的存档槽名
            if (saveData != null && !string.IsNullOrEmpty(saveData.saveSlotName))
            {
                if (_selectedSaveSlotName != saveData.saveSlotName)
                {
                    _selectedSaveSlotName = saveData.saveSlotName;
                    slotChanged = true;
                }
                
                if (_isAutoSaveSlot != saveData.isAutoSave)
                {
                    _isAutoSaveSlot = saveData.isAutoSave;
                    autoSaveChanged = true;
                }
            }
            else
            {
                if (_selectedSaveSlotName != slotName)
                {
                    _selectedSaveSlotName = slotName;
                    slotChanged = true;
                }
                
                // 使用配置中的自动存档槽名称判断是否为自动存档槽
                string autoSaveSlotName = "AutoSave";
                if (_config != null)
                {
                    autoSaveSlotName = _config.AutoSaveSlotName;
                }
                
                bool newIsAutoSave = slotName == autoSaveSlotName;
                if (_isAutoSaveSlot != newIsAutoSave)
                {
                    _isAutoSaveSlot = newIsAutoSave;
                    autoSaveChanged = true;
                }
            }
            
            if (_selectedSaveData != saveData)
            {
                _selectedSaveData = saveData;
                dataChanged = true;
            }
            
            // 通知属性变更
            if (slotChanged)
            {
                NotifyPropertyChanged(nameof(SelectedSaveSlotName));
            }
            
            if (autoSaveChanged)
            {
                NotifyPropertyChanged(nameof(IsAutoSaveSlot));
            }
            
            if (dataChanged)
            {
                NotifyPropertyChanged(nameof(SelectedSaveData));
            }
            
            // 只触发一次选中存档槽变更事件
            if (slotChanged || dataChanged)
            {
                OnSelectedSaveSlotChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// 更新存档槽列表
        /// </summary>
        /// <param name="slots">新的存档槽列表</param>
        public void UpdateSaveSlots(List<SaveSlotInfo> slots)
        {
            SaveSlots = new List<SaveSlotInfo>(slots);
        }
    }
    
    /// <summary>
    /// 存档槽信息结构
    /// 存储单个存档槽的元数据
    /// </summary>
    public class SaveSlotInfo
    {
        public string SlotName { get; set; }
        public string DisplayName { get; set; }
        public bool NotEmpty { get; set; }
        public string LastModified { get; set; }
        public string Version { get; set; }
        public string ProgressText { get; set; }
        public SaveData SaveData { get; set; }
        public bool IsAutoSave { get; set; }
    }
}