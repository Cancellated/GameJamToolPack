using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MyGame.Data;
using MyGame.UI.SaveLoad.Events;
using MyGame.UI.SaveLoad.Controller;
using MyGame.UI;
using Logger;

namespace MyGame.UI.SaveLoad.View
{
    /// <summary>
    /// 存档菜单视图接口
    /// 定义存档槽UI的基本操作
    /// </summary>
    public interface ISaveSlotUI
    {
        void Initialize(SaveSlotInfo slotInfo, SaveLoadMenuView view);
        void UpdateDisplay();
        void SetSelected(bool selected);
        string SlotName { get; }
    }

    /// <summary>
    /// 存档槽UI的抽象基类
    /// 提供存档槽UI的通用功能
    /// </summary>
    public abstract class SaveSlotUI : MonoBehaviour, ISaveSlotUI
    {
        [Header("Save Slot Components")]
        [SerializeField] protected Text slotNameText;
        [SerializeField] protected Text timestampText;
        [SerializeField] protected Text progressText;
        [SerializeField] protected Button slotButton;

        protected SaveSlotInfo _slotInfo;
        protected SaveLoadMenuView _view;
        protected bool _isSelected = false;

        /// <summary>
        /// 初始化存档槽UI
        /// </summary>
        /// <param name="slotInfo">存档槽信息</param>
        /// <param name="view">视图引用</param>
        public virtual void Initialize(SaveSlotInfo slotInfo, SaveLoadMenuView view)
        {
            _slotInfo = slotInfo;
            _view = view;

            if (slotButton != null)
            {
                slotButton.onClick.AddListener(HandleSlotButtonClick);
            }
            
            UpdateDisplay();
        }
        
        /// <summary>
        /// 更新存档槽显示
        /// </summary>
        public virtual void UpdateDisplay()
        {
            if (slotNameText != null)
            {
                slotNameText.text = _slotInfo.DisplayName;
            }
            
            if (timestampText != null)
            {
                timestampText.text = _slotInfo.HasSave ? _slotInfo.LastModified : "空存档槽";
            }
            
            if (progressText != null)
            {
                progressText.text = _slotInfo.HasSave ? _slotInfo.ProgressText : string.Empty;
            }
        }
        
        /// <summary>
        /// 设置存档槽选中状态
        /// </summary>
        /// <param name="selected">是否选中</param>
        public virtual void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateHighlight();
        }
        
        /// <summary>
        /// 获取存档槽名称
        /// </summary>
        public string SlotName
        {
            get { return _slotInfo != null ? _slotInfo.SlotName : string.Empty; }
        }
        
        /// <summary>
        /// 处理存档槽按钮点击事件
        /// 通知View层处理，View层再通过事件系统或Controller路由
        /// </summary>
        protected virtual void HandleSlotButtonClick()
        {
            if (_view != null)
            {
                _view.OnSaveSlotClick(_slotInfo.SlotName, _slotInfo.SaveData);
            }
        }
        
        /// <summary>
        /// 更新存档槽高亮状态
        /// </summary>
        protected abstract void UpdateHighlight();
        
        /// <summary>
        /// 清理资源
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (slotButton != null)
            {
                slotButton.onClick.RemoveAllListeners();
            }
        }
    }
    
    /// <summary>
    /// 存档菜单视图
    /// 负责显示存档菜单的UI元素并处理用户交互
    /// 所有Model数据访问均通过 Controller 中转，不直接持有Model引用
    /// </summary>
    public class SaveLoadMenuView : BaseView<SaveLoadMenuController>
    {
        [Header("Save Slots")]
        [SerializeField] protected Transform saveSlotsContainer;
        

        [Header("Save Options")]
        [SerializeField] protected GameObject saveOptionsMenu;
        [SerializeField] protected Button saveButton;
        [SerializeField] protected Button loadButton;
        [SerializeField] protected Button deleteButton;
        [SerializeField] protected Button cancelButton;

        [Header("Main Options")]
        [SerializeField] protected Button newGameButton;
        [SerializeField] protected Button backButton;

        protected List<ISaveSlotUI> _saveSlotUIs = new();

        private const string LOG_MODULE = LogModules.SAVELOAD;

        // 用于跟踪异步加载操作
        private List<AsyncOperationHandle<GameObject>> _prefabLoadHandles = new();

        /// <summary>
        /// 初始化面板：
        /// 设置面板类型并调用基类初始化
        /// </summary>
        protected override void Awake()
        {
            // 设置面板类型，必须在 base.Awake() 之前
            m_panelType = UIType.SaveLoadMenu;
            base.Awake();
        }

        /// <summary>
        /// 初始化视图
        /// 重写基类Initialize方法，绑定按钮事件并初始化UI状态
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            BindButtonEvents();
            HideSaveOptionsMenu();
        }

        /// <summary>
        /// 尝试自动绑定控制器
        /// View和Controller挂载在同一GameObject上，使用GetComponent查找
        /// </summary>
        protected override void TryBindController()
        {
            if (TryGetComponent<SaveLoadMenuController>(out var controller))
            {
                BindController(controller);
            }
        }

        /// <summary>
        /// 控制器绑定后的回调
        /// </summary>
        protected override void OnControllerBound()
        {
            base.OnControllerBound();
        }

        /// <summary>
        /// 控制器解绑后的回调
        /// </summary>
        protected override void OnControllerUnbound()
        {
            base.OnControllerUnbound();
        }

        /// <summary>
        /// 更新视图显示
        /// 由 Controller 在 Model 数据变更时调用
        /// </summary>
        public virtual void UpdateView()
        {
            CreateSaveSlotUIs();
            UpdateSaveOptionsButtonStates();
        }

        /// <summary>
        /// 创建存档槽UI
        /// 使用Addressable Assets异步加载预制件
        /// 数据通过 Controller 查询方法获取，不直接访问Model
        /// </summary>
        protected virtual async void CreateSaveSlotUIs()
        {
            if (m_controller == null || saveSlotsContainer == null)
                return;

            var config = m_controller.Config;
            if (config == null)
            {
                Log.Error(LOG_MODULE, "SaveLoadMenuConfig is not set in controller");
                return;
            }

            var saveSlots = m_controller.GetSaveSlots();
            if (saveSlots == null)
                return;

            // 清理现有存档槽UI和加载句柄
            ClearSaveSlotUIs();
            ClearLoadHandles();

            try
            {
                // 异步加载存档槽预制件
                AsyncOperationHandle<GameObject> prefabLoadHandle = Addressables.LoadAssetAsync<GameObject>(config.SaveSlotPrefabAddress);
                _prefabLoadHandles.Add(prefabLoadHandle);

                await prefabLoadHandle.Task;

                if (prefabLoadHandle.Status == AsyncOperationStatus.Succeeded && prefabLoadHandle.Result != null)
                {
                    GameObject saveSlotPrefab = prefabLoadHandle.Result;
                    string selectedSlotName = m_controller.GetSelectedSaveSlotName();

                    // 创建新的存档槽UI
                    foreach (var slotInfo in saveSlots)
                    {
                        GameObject slotGO = Instantiate(saveSlotPrefab, saveSlotsContainer);

                        if (slotGO.TryGetComponent<ISaveSlotUI>(out var slotUI))
                        {
                            slotUI.Initialize(slotInfo, this);
                            slotUI.SetSelected(selectedSlotName == slotInfo.SlotName);
                            _saveSlotUIs.Add(slotUI);
                        }
                    }
                }
                else
                {
                    Log.Error(LOG_MODULE, "加载存档槽预制件失败，状态: " + prefabLoadHandle.Status);
                }
            }
            catch (System.Exception e)
            {
                Log.Error(LOG_MODULE, "加载存档槽预制件时发生异常: " + e.Message);
            }
        }

        /// <summary>
        /// 清理异步加载句柄
        /// </summary>
        protected virtual void ClearLoadHandles()
        {
            foreach (var handle in _prefabLoadHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _prefabLoadHandles.Clear();
        }

        /// <summary>
        /// 清理存档槽UI
        /// </summary>
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

        /// <summary>
        /// 更新存档选项按钮状态
        /// 数据通过 Controller 查询方法获取
        /// </summary>
        protected virtual void UpdateSaveOptionsButtonStates()
        {
            if (m_controller == null)
                return;

            string selectedSlotName = m_controller.GetSelectedSaveSlotName();
            SaveData selectedSaveData = m_controller.GetSelectedSaveData();

            bool hasSelectedSlot = !string.IsNullOrEmpty(selectedSlotName);
            bool hasSaveData = selectedSaveData != null;

            if (saveButton != null)
            {
                saveButton.interactable = hasSelectedSlot;
            }

            if (loadButton != null)
            {
                loadButton.interactable = hasSaveData;
            }

            if (deleteButton != null)
            {
                deleteButton.interactable = hasSaveData;
            }
        }

        /// <summary>
        /// 绑定按钮事件
        /// </summary>
        protected virtual void BindButtonEvents()
        {
            if (saveButton != null)
            {
                saveButton.onClick.AddListener(HandleSaveButtonClick);
            }

            if (loadButton != null)
            {
                loadButton.onClick.AddListener(HandleLoadButtonClick);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(HandleDeleteButtonClick);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HandleCancelButtonClick);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(HandleNewGameButtonClick);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBackButtonClick);
            }
        }

        /// <summary>
        /// 解绑按钮事件
        /// </summary>
        protected virtual void UnbindButtonEvents()
        {
            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(HandleSaveButtonClick);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(HandleLoadButtonClick);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(HandleDeleteButtonClick);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelButtonClick);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(HandleNewGameButtonClick);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBackButtonClick);
            }
        }

        /// <summary>
        /// 处理存档槽点击事件
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        /// <param name="saveData">存档数据</param>
        public virtual void OnSaveSlotClick(string slotName, SaveData saveData = null)
        {
            // 触发存档槽选中事件
            SaveLoadMenuEvents.TriggerSaveSlotSelected(slotName, saveData);

            if (saveData == null)
            {
                // 空槽位直接存档
                SaveLoadMenuEvents.TriggerSaveGame(slotName);
            }
            else
            {
                // 有存档时显示操作菜单
                ShowSaveOptionsMenu();
            }
        }

        /// <summary>
        /// 处理存档按钮点击事件
        /// 通过 Controller 获取选中的存档槽名称
        /// </summary>
        protected virtual void HandleSaveButtonClick()
        {
            string slotName = m_controller?.GetSelectedSaveSlotName();
            if (!string.IsNullOrEmpty(slotName))
            {
                SaveLoadMenuEvents.TriggerSaveGame(slotName);
                HideSaveOptionsMenu();
            }
        }

        /// <summary>
        /// 处理加载按钮点击事件
        /// 通过 Controller 获取选中的存档槽名称
        /// </summary>
        protected virtual void HandleLoadButtonClick()
        {
            string slotName = m_controller?.GetSelectedSaveSlotName();
            if (!string.IsNullOrEmpty(slotName))
            {
                SaveLoadMenuEvents.TriggerLoadGame(slotName);
                HideSaveOptionsMenu();
            }
        }

        /// <summary>
        /// 处理删除按钮点击事件
        /// 通过 Controller 获取选中的存档槽名称
        /// </summary>
        protected virtual void HandleDeleteButtonClick()
        {
            string slotName = m_controller?.GetSelectedSaveSlotName();
            if (!string.IsNullOrEmpty(slotName))
            {
                SaveLoadMenuEvents.TriggerDeleteSave(slotName);
                HideSaveOptionsMenu();
            }
        }

        /// <summary>
        /// 处理取消按钮点击事件
        /// </summary>
        protected virtual void HandleCancelButtonClick()
        {
            HideSaveOptionsMenu();
        }

        /// <summary>
        /// 处理新游戏按钮点击事件
        /// </summary>
        protected virtual void HandleNewGameButtonClick()
        {
            SaveLoadMenuEvents.TriggerCreateNewGame();
        }

        /// <summary>
        /// 处理返回按钮点击事件
        /// </summary>
        protected virtual void HandleBackButtonClick()
        {
            SaveLoadMenuEvents.TriggerBackToMainMenu();
        }

        /// <summary>
        /// 显示存档选项菜单
        /// </summary>
        public virtual void ShowSaveOptionsMenu()
        {
            if (saveOptionsMenu != null)
            {
                saveOptionsMenu.SetActive(true);
            }
        }

        /// <summary>
        /// 隐藏存档选项菜单
        /// </summary>
        public virtual void HideSaveOptionsMenu()
        {
            if (saveOptionsMenu != null)
            {
                saveOptionsMenu.SetActive(false);
            }
        }

        /// <summary>
        /// 清理资源
        /// 解绑按钮事件、清理存档槽UI和Addressable资源
        /// </summary>
        protected override void OnDestroy()
        {
            UnbindButtonEvents();
            ClearSaveSlotUIs();
            ClearLoadHandles();
            base.OnDestroy();
        }
    }
}
