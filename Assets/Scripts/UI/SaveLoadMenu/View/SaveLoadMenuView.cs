using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MyGame.Data;
using MyGame.Managers;
using MyGame.UI.SaveLoad.Controller;
using MyGame.UI;
using Logger;
using TMPro;

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
        [Tooltip("新游戏按钮位于空槽详情面板中；选中已有存档时自动隐藏")]
        [SerializeField] protected Button newGameButton;

        [Header("Main Options")]
        [SerializeField] protected Button backButton;

        [Header("Confirmation Dialog")]
        [Tooltip("确认弹窗根节点（可选；缺失时会按子物体名自动查找）")]
        [SerializeField] protected GameObject confirmDialog;
        [SerializeField] protected TextMeshProUGUI confirmTitleText;
        [SerializeField] protected TextMeshProUGUI confirmMessageText;
        [SerializeField] protected Button confirmButton;
        [SerializeField] protected Button confirmCancelButton;

        [Header("Runtime Fallback")]
        [Tooltip("运行时兜底生成的详情面板中的选中信息文本")]
        [SerializeField] protected Text fallbackSelectedInfoText;

        protected List<ISaveSlotUI> _saveSlotUIs = new();

        private const string LOG_MODULE = LogModules.SAVELOAD;

        // 用于跟踪异步加载操作
        private List<AsyncOperationHandle<GameObject>> _prefabLoadHandles = new();

        /// <summary>槽位构建代数：每次请求重建时 +1，用于丢弃过期异步结果，防止槽位翻倍。</summary>
        private int _slotBuildGeneration;

        /// <summary>确认面板当前等待执行的操作</summary>
        private enum PendingSaveAction
        {
            None,
            Save,
            Load,
            Delete,
        }

        private PendingSaveAction _pendingAction = PendingSaveAction.None;
        private string _pendingSlotName;

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
        /// 面板启用时监听 InputSystem 的 UI.Cancel（Esc），用于逐级返回：
        /// 确认弹窗 → 槽位详情面板 → 上一级菜单。
        /// InputActions 属性自举初始化，不依赖其它管理器 Awake 顺序。
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            TrySubscribeCancelInput();
        }

        protected override void OnDisable()
        {
            TryUnsubscribeCancelInput();
            base.OnDisable();
        }

        /// <summary>
        /// 初始化视图：绑定按钮事件并初始化UI状态
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // 场景/预制体缺失控件时生成运行时兜底 UI，保证菜单可用
            EnsureFallbackUI();
            EnsureConfirmationReferences();

            BindButtonEvents();
            HideSaveOptionsMenu();
            HideConfirmationDialog();
        }

        /// <summary>
        /// 清理面板资源：解绑按钮事件、清理存档槽UI与Addressable句柄
        /// </summary>
        public override void Cleanup()
        {
            UnbindButtonEvents();
            ClearSaveSlotUIs();
            ClearLoadHandles();
            base.Cleanup();
        }

        /// <summary>
        /// 尝试自动绑定控制器（标准模式：同物体查找，初始化并注入视图）
        /// </summary>
        protected override void TryBindController()
        {
            if (TryGetComponent<SaveLoadMenuController>(out var controller))
            {
                // 找到预挂的控制器：初始化、注入视图并绑定
                controller.Initialize();
                controller.SetView(this);
                BindController(controller);
                return;
            }

            // 未预挂时创建（仅作为防御路径，prefab 上应始终预挂）
            controller = gameObject.AddComponent<SaveLoadMenuController>();
            controller.Initialize();
            controller.SetView(this);

            // 绑定控制器到视图
            BindController(controller);
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
        /// 仅刷新选中态相关 UI：槽位高亮、详情文本与操作按钮可用状态。
        /// 不重建槽位列表，供 Controller 在选中属性变化时调用。
        /// </summary>
        public virtual void RefreshSelectedState()
        {
            RefreshSlotSelection();
            UpdateSaveOptionsButtonStates();
            UpdateSelectedSlotInfo();
        }

        /// <summary>
        /// 按当前选中槽名刷新已有槽位 UI 的高亮状态。
        /// </summary>
        protected void RefreshSlotSelection()
        {
            if (m_controller == null)
            {
                return;
            }

            string selectedSlotName = m_controller.GetSelectedSaveSlotName();
            foreach (ISaveSlotUI slotUI in _saveSlotUIs)
            {
                if (slotUI is not null)
                {
                    slotUI.SetSelected(!string.IsNullOrEmpty(selectedSlotName) && slotUI.SlotName == selectedSlotName);
                }
            }
        }

        /// <summary>
        /// 更新选中槽位详情文本（运行时兜底详情面板使用）。
        /// 正式 TMP 详情面板由 SaveLoadMenuPanel 覆写实现。
        /// </summary>
        protected virtual void UpdateSelectedSlotInfo()
        {
            if (fallbackSelectedInfoText == null || m_controller == null)
            {
                return;
            }

            string selectedSlotName = m_controller.GetSelectedSaveSlotName();
            if (string.IsNullOrEmpty(selectedSlotName))
            {
                fallbackSelectedInfoText.text = "请选择一个存档";
                return;
            }

            SaveSlotInfo selectedSlotInfo = null;
            var saveSlots = m_controller.GetSaveSlots();
            if (saveSlots != null)
            {
                foreach (var slotInfo in saveSlots)
                {
                    if (slotInfo.SlotName == selectedSlotName)
                    {
                        selectedSlotInfo = slotInfo;
                        break;
                    }
                }
            }

            if (selectedSlotInfo == null)
            {
                fallbackSelectedInfoText.text = $"选中存档：{selectedSlotName}\n(空存档槽)";
                return;
            }

            string info = $"选中存档：{selectedSlotInfo.DisplayName}\n";
            if (selectedSlotInfo.SaveData != null)
            {
                info += $"存档时间：{selectedSlotInfo.LastModified}\n";
                info += $"游戏版本：{selectedSlotInfo.Version}\n";
                info += string.IsNullOrEmpty(selectedSlotInfo.ProgressText)
                    ? "进度：暂无"
                    : $"进度：{selectedSlotInfo.ProgressText}";
            }
            else
            {
                info += "(空存档槽)";
            }

            fallbackSelectedInfoText.text = info;
        }

        /// <summary>
        /// 创建存档槽UI
        /// 优先使用Addressable Assets异步加载预制件；配置缺失或加载失败时使用运行时兜底槽位，
        /// 避免因 SaveSlotPrefab 未配置而导致整个菜单不可用
        /// </summary>
        protected virtual async void CreateSaveSlotUIs()
        {
            if (m_controller == null)
                return;

            var saveSlots = m_controller.GetSaveSlots();
            if (saveSlots == null)
                return;

            // 递增代数：只允许最后一次请求生成槽位。
            // 选中槽位会连续触发多个 Model 属性变更，每次变更都会触发一次 UpdateView；
            // 若不加代数校验，多个异步加载任务都可能完成并各生成一套槽位，导致数量翻倍。
            int buildId = ++_slotBuildGeneration;

            var config = m_controller.Config;
            if (config == null || string.IsNullOrEmpty(config.SaveSlotPrefabAddress))
            {
                Log.Warning(LOG_MODULE, "SaveLoadMenuConfig 或 SaveSlotPrefabAddress 未配置，使用运行时兜底存档槽");
                CreateFallbackSaveSlotUIs(saveSlots);
                return;
            }

            EnsureFallbackSlotContainer();

            // 清理现有存档槽UI和加载句柄
            ClearSaveSlotUIs();
            ClearLoadHandles();

            try
            {
                // 异步加载存档槽预制件
                AsyncOperationHandle<GameObject> prefabLoadHandle = Addressables.LoadAssetAsync<GameObject>(config.SaveSlotPrefabAddress);
                _prefabLoadHandles.Add(prefabLoadHandle);

                await prefabLoadHandle.Task;

                // 防御异步竞态：
                // 1) 面板已销毁；
                // 2) 等待期间又有新的重建请求，本代结果过期；
                // 3) 句柄已被新一轮清理流程 Release。
                // 三者任一成立都必须放弃本次生成，否则会出现成倍的槽位实例。
                if (this == null || buildId != _slotBuildGeneration || !prefabLoadHandle.IsValid())
                {
                    return;
                }

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
                        else
                        {
                            Destroy(slotGO);
                        }
                    }
                }
                else
                {
                    Log.Warning(LOG_MODULE, "加载存档槽预制件失败，状态: " + prefabLoadHandle.Status + "，使用运行时兜底存档槽");
                    CreateFallbackSaveSlotUIs(saveSlots);
                }
            }
            catch (System.Exception e)
            {
                if (this == null || saveSlotsContainer == null || buildId != _slotBuildGeneration)
                {
                    return;
                }

                Log.Error(LOG_MODULE, "加载存档槽预制件时发生异常: " + e.Message + "，使用运行时兜底存档槽");
                CreateFallbackSaveSlotUIs(saveSlots);
            }
        }

        /// <summary>
        /// 运行时兜底：生成存档槽、操作按钮和槽位容器。
        /// 用于场景/预制体中引用缺失，或 Addressable 资源不可用时，保证菜单仍可操作。
        /// </summary>
        private void CreateFallbackSaveSlotUIs(List<SaveSlotInfo> saveSlots)
        {
            if (saveSlots == null)
                return;

            EnsureFallbackSlotContainer();

            ClearSaveSlotUIs();
            ClearLoadHandles();

            string selectedSlotName = m_controller != null ? m_controller.GetSelectedSaveSlotName() : null;

            foreach (var slotInfo in saveSlots)
            {
                GameObject slotGO = new GameObject("SaveSlot_" + slotInfo.SlotName,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(RuntimeSaveSlotUI));
                slotGO.transform.SetParent(saveSlotsContainer, false);

                RuntimeSaveSlotUI slotUI = slotGO.GetComponent<RuntimeSaveSlotUI>();
                slotUI.Initialize(slotInfo, this);
                slotUI.SetSelected(selectedSlotName == slotInfo.SlotName);
                _saveSlotUIs.Add(slotUI);
            }
        }

        /// <summary>
        /// 运行时兜底：槽位容器缺失时在面板内创建一个纵向列表容器。
        /// </summary>
        private void EnsureFallbackSlotContainer()
        {
            if (saveSlotsContainer != null)
                return;

            GameObject containerGO = new GameObject("FallbackSaveSlotsContainer",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            containerGO.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)containerGO.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(24f, 180f);
            rect.offsetMax = new Vector2(-24f, -120f);

            VerticalLayoutGroup layout = containerGO.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = containerGO.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            saveSlotsContainer = containerGO.transform;
        }

        /// <summary>
        /// 运行时兜底：检查并生成缺失的操作按钮与选项菜单。
        /// </summary>
        private void EnsureFallbackUI()
        {
            // 防御旧的错误场景接线：saveOptionsMenu 不能是面板自身
            if (saveOptionsMenu == gameObject)
            {
                saveOptionsMenu = null;
            }

            bool missingOptionsMenu = saveOptionsMenu == null
                                      || saveButton == null
                                      || loadButton == null
                                      || deleteButton == null
                                      || cancelButton == null
                                      || newGameButton == null;

            if (missingOptionsMenu)
            {
                CreateFallbackOptionsMenu();
            }

            if (backButton == null)
            {
                CreateFallbackMainButtons();
            }
        }

        /// <summary>
        /// 运行时兜底：生成居中的槽位详情面板（含详情文本与保存/读取/删除/取消按钮）。
        /// </summary>
        private void CreateFallbackOptionsMenu()
        {
            // 丢弃不完整的旧引用，统一重建
            saveOptionsMenu = null;
            saveButton = null;
            loadButton = null;
            deleteButton = null;
            cancelButton = null;
            newGameButton = null;
            fallbackSelectedInfoText = null;

            GameObject panelGO = new GameObject("SlotDetailsPanel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelGO.transform.SetParent(transform, false);

            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(620f, 380f);

            Image background = panelGO.GetComponent<Image>();
            background.color = new Color(0.12f, 0.12f, 0.14f, 0.97f);

            Text titleText = CreateRuntimeText("Title", "存档详情", panelGO.transform);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontSize = 26;
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(16f, -52f);
            titleRect.offsetMax = new Vector2(-16f, -8f);

            fallbackSelectedInfoText = CreateRuntimeText("Info", "请选择一个存档", panelGO.transform);
            fallbackSelectedInfoText.fontSize = 18;
            RectTransform infoRect = fallbackSelectedInfoText.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0f, 0f);
            infoRect.anchorMax = new Vector2(1f, 1f);
            infoRect.offsetMin = new Vector2(24f, 84f);
            infoRect.offsetMax = new Vector2(-24f, -64f);

            GameObject buttonBar = new GameObject("Buttons",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonBar.transform.SetParent(panelGO.transform, false);

            RectTransform buttonBarRect = buttonBar.GetComponent<RectTransform>();
            buttonBarRect.anchorMin = new Vector2(0f, 0f);
            buttonBarRect.anchorMax = new Vector2(1f, 0f);
            buttonBarRect.pivot = new Vector2(0.5f, 0f);
            buttonBarRect.anchoredPosition = Vector2.zero;
            buttonBarRect.sizeDelta = new Vector2(0f, 64f);

            HorizontalLayoutGroup layout = buttonBar.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            saveOptionsMenu = panelGO;
            newGameButton = CreateRuntimeButton("NewGameButton", "新游戏", buttonBar.transform);
            saveButton = CreateRuntimeButton("SaveButton", "保存存档", buttonBar.transform);
            loadButton = CreateRuntimeButton("LoadButton", "读取存档", buttonBar.transform);
            deleteButton = CreateRuntimeButton("DeleteButton", "删除存档", buttonBar.transform);
            cancelButton = CreateRuntimeButton("CancelButton", "取消", buttonBar.transform);

            // 详情面板默认关闭
            saveOptionsMenu.SetActive(false);
        }

        /// <summary>
        /// 自动查找确认弹窗引用：优先使用 Inspector 已拖入的引用；
        /// 未拖入时按固定子物体名查找（兼容 MainMenu 场景里已设计好的 ConfirmDialog）。
        /// </summary>
        private void EnsureConfirmationReferences()
        {
            if (confirmDialog == null)
            {
                Transform dialogTransform = transform.Find("ConfirmDialog");
                confirmDialog = dialogTransform != null ? dialogTransform.gameObject : null;
            }

            if (confirmDialog == null)
            {
                return;
            }

            if (confirmTitleText == null)
            {
                Transform title = confirmDialog.transform.Find("Title");
                confirmTitleText = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
            }

            if (confirmMessageText == null)
            {
                Transform content = confirmDialog.transform.Find("Content");
                confirmMessageText = content != null ? content.GetComponent<TextMeshProUGUI>() : null;
            }

            if (confirmButton == null)
            {
                Transform confirm = confirmDialog.transform.Find("Confirm");
                confirmButton = confirm != null ? confirm.GetComponent<Button>() : null;
            }

            if (confirmCancelButton == null)
            {
                Transform cancel = confirmDialog.transform.Find("Cancel");
                confirmCancelButton = cancel != null ? cancel.GetComponent<Button>() : null;
            }
        }

        /// <summary>
        /// 运行时兜底：返回主菜单放在左上角。
        /// 新游戏按钮不再放在主界面，只出现在空槽详情面板中。
        /// </summary>
        private void CreateFallbackMainButtons()
        {
            if (backButton == null)
            {
                backButton = CreateRuntimeButton("BackButton", "返回主菜单", transform);
                RectTransform rect = backButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(24f, -24f);
                rect.sizeDelta = new Vector2(200f, 48f);
            }
        }

        /// <summary>
        /// 运行时兜底：创建标准 uGUI 按钮（包含背景、Button 与文字）。
        /// </summary>
        private static Button CreateRuntimeButton(string name, string label, Transform parent)
        {
            GameObject buttonGO = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);

            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = buttonGO.GetComponent<Image>();
            background.color = new Color(0.25f, 0.25f, 0.28f, 0.98f);

            Button button = buttonGO.GetComponent<Button>();
            button.targetGraphic = background;

            Text labelText = CreateRuntimeText("Label", label, buttonGO.transform);
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.fontSize = 20;

            return button;
        }

        /// <summary>
        /// 运行时兜底：创建标准 uGUI 文本。
        /// </summary>
        private static Text CreateRuntimeText(string name, string text, Transform parent)
        {
            GameObject textGO = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGO.transform.SetParent(parent, false);

            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6f, 2f);
            rect.offsetMax = new Vector2(-6f, -2f);

            Text label = textGO.GetComponent<Text>();
            label.text = text;
            label.color = Color.white;
            label.raycastTarget = false;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            return label;
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

            // 同时清除槽位容器中残留的编辑器预览占位槽：
            // Prefab 中会放置 PreviewSlot_* 用于预览，运行时必须移除后再实例化真实槽位
            if (saveSlotsContainer != null)
            {
                for (int i = saveSlotsContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = saveSlotsContainer.GetChild(i);
                    if (child.name.StartsWith("PreviewSlot_", System.StringComparison.Ordinal))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
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

            // 新游戏按钮只出现在空槽详情面板中
            if (newGameButton != null)
            {
                bool showNewGame = hasSelectedSlot && !hasSaveData;
                if (newGameButton.gameObject.activeSelf != showNewGame)
                {
                    newGameButton.gameObject.SetActive(showNewGame);
                }
                newGameButton.interactable = showNewGame;
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

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirmButtonClick);
            }

            if (confirmCancelButton != null)
            {
                confirmCancelButton.onClick.AddListener(HandleConfirmCancelButtonClick);
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

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmButtonClick);
            }

            if (confirmCancelButton != null)
            {
                confirmCancelButton.onClick.RemoveListener(HandleConfirmCancelButtonClick);
            }
        }

        /// <summary>
        /// 处理存档槽点击事件
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        /// <param name="saveData">存档数据</param>
        public virtual void OnSaveSlotClick(string slotName, SaveData saveData = null)
        {
            if (m_controller == null)
                return;

            // 通知 Controller 记录选中槽位
            m_controller.HandleSaveSlotSelected(slotName, saveData);

            // 统一弹出槽位详情与操作面板；空槽/有档槽都走面板操作
            ShowSaveOptionsMenu();
        }

        /// <summary>
        /// 处理存档按钮点击事件：覆盖已有存档时先弹确认框
        /// </summary>
        protected virtual void HandleSaveButtonClick()
        {
            string slotName = m_controller?.GetSelectedSaveSlotName();
            if (string.IsNullOrEmpty(slotName))
            {
                return;
            }

            SaveData selectedData = m_controller.GetSelectedSaveData();
            if (selectedData != null)
            {
                RequestConfirmation(PendingSaveAction.Save, slotName,
                    $"将覆盖存档槽“{slotName}”中的存档，确定继续吗？");
                return;
            }

            ExecuteSave(slotName);
        }

        /// <summary>
        /// 处理加载按钮点击事件：读取会覆盖当前游戏进度，先确认
        /// </summary>
        protected virtual void HandleLoadButtonClick()
        {
            string slotName = m_controller?.GetSelectedSaveSlotName();
            if (string.IsNullOrEmpty(slotName))
            {
                return;
            }

            RequestConfirmation(PendingSaveAction.Load, slotName,
                $"读取“{slotName}”将覆盖当前游戏进度，确定继续吗？");
        }

        /// <summary>
        /// 处理删除按钮点击事件：删除不可恢复，必须先确认
        /// </summary>
        protected virtual void HandleDeleteButtonClick()
        {
            string slotName = m_controller?.GetSelectedSaveSlotName();
            if (string.IsNullOrEmpty(slotName))
            {
                return;
            }

            RequestConfirmation(PendingSaveAction.Delete, slotName,
                $"删除“{slotName}”后不可恢复，确定删除吗？");
        }

        /// <summary>
        /// 处理取消按钮点击事件
        /// </summary>
        protected virtual void HandleCancelButtonClick()
        {
            HideSaveOptionsMenu();
        }

        /// <summary>
        /// 请求确认：优先使用确认面板；面板缺失时为保证可用直接执行操作
        /// </summary>
        private void RequestConfirmation(PendingSaveAction action, string slotName, string message)
        {
            _pendingAction = action;
            _pendingSlotName = slotName;

            if (confirmDialog == null || confirmButton == null)
            {
                Log.Warning(LOG_MODULE, "确认面板未配置，跳过确认直接执行操作");
                ExecutePendingAction();
                return;
            }

            if (confirmTitleText != null)
            {
                confirmTitleText.text = "确认";
            }

            if (confirmMessageText != null)
            {
                confirmMessageText.text = message;
            }

            HideSaveOptionsMenu();
            confirmDialog.SetActive(true);
        }

        /// <summary>
        /// 确认面板的“确定”按钮：执行等待中的存档操作
        /// </summary>
        private void HandleConfirmButtonClick()
        {
            ExecutePendingAction();
        }

        /// <summary>
        /// 确认面板的“取消”按钮：关闭确认面板并重新显示选项菜单
        /// </summary>
        private void HandleConfirmCancelButtonClick()
        {
            HideConfirmationDialog();

            if (!string.IsNullOrEmpty(_pendingSlotName))
            {
                ShowSaveOptionsMenu();
            }
        }

        private void ExecutePendingAction()
        {
            if (string.IsNullOrEmpty(_pendingSlotName))
            {
                HideConfirmationDialog();
                return;
            }

            PendingSaveAction action = _pendingAction;
            string slotName = _pendingSlotName;
            _pendingAction = PendingSaveAction.None;
            _pendingSlotName = null;

            HideConfirmationDialog();
            HideSaveOptionsMenu();

            switch (action)
            {
                case PendingSaveAction.Save:
                    ExecuteSave(slotName);
                    break;
                case PendingSaveAction.Load:
                    m_controller?.HandleLoadGame(slotName);
                    break;
                case PendingSaveAction.Delete:
                    m_controller?.HandleDeleteSave(slotName);
                    break;
            }
        }

        private void ExecuteSave(string slotName)
        {
            m_controller?.HandleSaveGame(slotName);
            HideSaveOptionsMenu();
        }

        /// <summary>
        /// 隐藏确认面板
        /// </summary>
        protected void HideConfirmationDialog()
        {
            if (confirmDialog != null && confirmDialog != gameObject)
            {
                confirmDialog.SetActive(false);
            }
        }

        /// <summary>
        /// 订阅 InputSystem UI.Cancel 输入（幂等）。
        /// </summary>
        protected void TrySubscribeCancelInput()
        {
            if (InputManager.Instance == null || InputManager.Instance.InputActions == null)
            {
                Log.Warning(LOG_MODULE, "无法订阅 UI.Cancel：InputManager 不可用");
                return;
            }

            InputManager.Instance.InputActions.UI.Cancel.performed -= HandleUICancelPerformed;
            InputManager.Instance.InputActions.UI.Cancel.performed += HandleUICancelPerformed;
            Log.Info(LOG_MODULE, "已订阅 InputSystem UI.Cancel（Esc）");
        }

        /// <summary>
        /// 重新绑定交互并订阅 Cancel 输入。
        /// 面板 Hide 后再次 Show 时调用，确保按钮监听不会因生命周期原因丢失。
        /// </summary>
        protected void EnsureInteractionBindings()
        {
            // 先解绑再绑定，保证幂等，不会重复触发
            UnbindButtonEvents();
            BindButtonEvents();
            TrySubscribeCancelInput();
        }

        /// <summary>
        /// 退订 InputSystem UI.Cancel 输入（幂等）。
        /// </summary>
        private void TryUnsubscribeCancelInput()
        {
            if (InputManager.Instance == null || InputManager.Instance.InputActions == null)
            {
                return;
            }

            InputManager.Instance.InputActions.UI.Cancel.performed -= HandleUICancelPerformed;
        }

        /// <summary>
        /// Esc/Cancel 返回逻辑：先关确认弹窗，再关详情面板，最后退回上一级菜单。
        /// </summary>
        private void HandleUICancelPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            Log.Info(LOG_MODULE, "收到 UI.Cancel 输入，执行逐级返回");

            if (confirmDialog != null && confirmDialog.activeSelf)
            {
                HandleConfirmCancelButtonClick();
                return;
            }

            if (saveOptionsMenu != null && saveOptionsMenu != gameObject && saveOptionsMenu.activeSelf)
            {
                HideSaveOptionsMenu();
                return;
            }

            m_controller?.HandleBackToMainMenu();
        }

        /// <summary>
        /// 处理新游戏按钮点击事件（仅空槽详情面板中显示）
        /// </summary>
        protected virtual void HandleNewGameButtonClick()
        {
            HideSaveOptionsMenu();
            HideConfirmationDialog();
            m_controller?.HandleCreateNewGame();
        }

        /// <summary>
        /// 处理返回按钮点击事件
        /// </summary>
        protected virtual void HandleBackButtonClick()
        {
            m_controller?.HandleBackToMainMenu();
        }

        /// <summary>
        /// 显示存档选项菜单
        /// </summary>
        public virtual void ShowSaveOptionsMenu()
        {
            // 先刷新详情文本，再显示详情面板
            UpdateSelectedSlotInfo();

            // 防御错误接线：选项菜单绝不能是面板根节点，否则会把整个面板隐藏
            if (saveOptionsMenu != null && saveOptionsMenu != gameObject)
            {
                saveOptionsMenu.SetActive(true);
            }
        }

        /// <summary>
        /// 隐藏存档选项菜单
        /// </summary>
        public virtual void HideSaveOptionsMenu()
        {
            // 防御错误接线：选项菜单绝不能是面板根节点，否则会把整个面板隐藏
            if (saveOptionsMenu != null && saveOptionsMenu != gameObject)
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
