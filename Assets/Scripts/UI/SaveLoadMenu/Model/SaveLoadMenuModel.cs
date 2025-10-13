using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Data;
using MyGame.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Logger;

namespace MyGame.UI.SaveLoad.Model
{
    /// <summary>
    /// 存档菜单的数据模型类，继承自ObservableModel
    /// 用于管理和提供存档菜单所需的数据
    /// </summary>
    public class SaveLoadMenuModel : ObservableModel
    {
        /// <summary>
        /// 存档菜单模式枚举
        /// </summary>
        public enum MenuMode
        {
            Save,
            Load
        }

        #region 成员变量
        private SaveData _selectedSaveData;
        private string _selectedSaveSlotName;
        private bool _isAutoSaveSlot;
        private List<SaveSlotInfo> _saveSlots = new();
        private MenuMode _currentMenuMode = MenuMode.Save;
        
        // 配置引用
        private SaveLoadMenuConfig _config;
        
        // 分页相关变量
        private int _currentPage = 0;
        private int _totalPages = 0;
        #endregion
        
        #region 事件
        /// <summary>
        /// 存档槽信息更新事件
        /// </summary>
        public event System.Action OnSaveSlotsUpdated;
        
        /// <summary>
        /// 选中存档槽变更事件
        /// </summary>
        public event System.Action OnSelectedSaveSlotChanged;
        
        /// <summary>
        /// 页码变更事件
        /// </summary>
        public event System.Action OnPageChanged;
        
        /// <summary>
        /// 菜单模式变更事件
        /// </summary>
        public event System.Action<MenuMode> OnMenuModeChanged;
        #endregion
        
        #region 属性
        /// <summary>
        /// 获取当前页的存档槽列表
        /// </summary>
        public List<SaveSlotInfo> CurrentPageSaveSlots
        {
            get
            {
                int startIndex = _currentPage * _config.SlotsPerPage;
                int endIndex = Mathf.Min(startIndex + _config.SlotsPerPage, _saveSlots.Count);
                return _saveSlots.GetRange(startIndex, endIndex - startIndex);
            }
        }

        /// <summary>
        /// 获取当前页的存档槽信息
        /// </summary>
        public List<SaveSlotInfo> GetCurrentPageSaveSlots
        {
            get { return CurrentPageSaveSlots; }
        }

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
        /// 当前菜单模式（保存或读取）
        /// </summary>
        public MenuMode CurrentMenuMode
        {
            get { return _currentMenuMode; }
            set 
            {
                if (_currentMenuMode != value)
                {
                    SetProperty(ref _currentMenuMode, value, nameof(CurrentMenuMode));
                    OnMenuModeChanged?.Invoke(value);
                }
            }
        }
        
        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get { return _currentPage; }
            private set 
            {
                if (_currentPage != value)
                {
                    _currentPage = Mathf.Clamp(value, 0, _totalPages - 1);
                    OnPageChanged?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get { return _totalPages; }
            private set { _totalPages = Mathf.Max(1, value); }
        }
        
        #endregion
        
        #region 初始化和清理方法
        /// <summary>
        /// 初始化模型
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            
            // 使用Addressable Assets加载配置文件
            LoadConfigAsync();
            
            // 初始化数据
            _saveSlots = new List<SaveSlotInfo>();
        }
        
        /// <summary>
        /// 使用Addressable Assets加载配置文件
        /// </summary>
        private async void LoadConfigAsync()
        {
            try
            {
                // 加载Addressable配置文件
                AsyncOperationHandle<SaveLoadMenuConfig> handle = Addressables.LoadAssetAsync<SaveLoadMenuConfig>("SaveLoadMenuConfig");
                _config = await handle.Task;
                
                if (_config == null)
                {
                    Log.Warning(LogModules.SAVE, "存档界面配置文件未找到，将创建默认配置");
                    _config = ScriptableObject.CreateInstance<SaveLoadMenuConfig>();
                }
                
                // 释放句柄
                Addressables.Release(handle);
            }
            catch (System.Exception e)
            {
                Log.Error(LogModules.SAVE, "读取配置失败: " + e.Message);
                // 创建默认配置作为后备方案
                _config = ScriptableObject.CreateInstance<SaveLoadMenuConfig>();
            }
        }

        /// <summary>
        /// 清理模型资源
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
            // 清理资源
            _saveSlots?.Clear();
            _selectedSaveData = null;
            _selectedSaveSlotName = null;
        }
        #endregion
        
        #region 配置访问方法
        
        /// <summary>
        /// 获取自动存档槽名称
        /// </summary>
        public string AutoSaveSlotName => _config != null ? _config.AutoSaveSlotName : "AutoSave";
        
        /// <summary>
        /// 获取默认存档槽数量
        /// </summary>
        public int DefaultSaveSlotCount => _config != null ? _config.DefaultSaveSlotCount : 3;
        
        /// <summary>
        /// 获取配置文件引用
        /// </summary>
        public SaveLoadMenuConfig Config => _config;
        #endregion
        
        #region 存档槽操作方法
        /// <summary>
        /// 设置选中的存档槽
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        /// <param name="saveData">存档数据</param>
        public void SetSelectedSaveSlot(string slotName, SaveData saveData = null)
        {
            SelectedSaveSlotName = slotName;
            SelectedSaveData = saveData;
            IsAutoSaveSlot = slotName == AutoSaveSlotName;
        }
        
        /// <summary>
        /// 更新存档槽列表
        /// </summary>
        /// <param name="slots">新的存档槽列表</param>
        public void UpdateSaveSlots(List<SaveSlotInfo> slots)
        {
            SaveSlots = new List<SaveSlotInfo>(slots);
        }
        
        /// <summary>
        /// 设置菜单模式
        /// </summary>
        /// <param name="mode">菜单模式</param>
        public void SetMenuMode(MenuMode mode)
        {
            CurrentMenuMode = mode;
        }
        #endregion
        
        #region 分页操作方法
        
        /// <summary>
        /// 跳转到上一页
        /// </summary>
        public void GoToPreviousPage()
        {
            if (_currentPage > 0)
            {
                CurrentPage--;
            }
        }
        
        /// <summary>
        /// 跳转到下一页
        /// </summary>
        public void GoToNextPage()
        {
            if (_currentPage < _totalPages - 1)
            {
                CurrentPage++;
            }
        }
        
        /// <summary>
        /// 跳转到指定页码
        /// </summary>
        /// <param name="pageIndex">目标页码（从0开始）</param>
        public void GoToPage(int pageIndex)
        {
            CurrentPage = pageIndex;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 存档槽信息结构
    /// 存储单个存档槽的元数据
    /// </summary>
    public class SaveSlotInfo
    {
        public string SlotName { get; set; }
        public string DisplayName { get; set; }
        public bool HasSave { get; set; }
        public string LastModified { get; set; }
        public string Version { get; set; }
        public string ProgressText { get; set; }
        public SaveData SaveData { get; set; }
        public bool IsAutoSave { get; set; }
    }
}