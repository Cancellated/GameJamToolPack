using UnityEngine;
using Inventory.data;
using Inventory.view;
using MyGame.Events;
using MyGame.UI;
using Logger;

namespace Inventory.controller
{
    /// <summary>
    /// 背包控制器类，负责处理背包的逻辑和数据管理
    /// 作为MVC架构中的控制器层，继承BaseController以遵循MVC规范
    /// </summary>
    public class InventoryController : BaseController<InventoryView, InventoryModel>
    {
        #region 字段

        [Tooltip("物品数据库")]
        [SerializeField] private ItemDatabase itemDatabase;

        private GameControl _inputActions;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化控制器
        /// </summary>
        private void Awake()
        {
            _inputActions = new GameControl();
            InitializeMVCComponents();
            Initialize();
        }

        /// <summary>
        /// 启用控制器
        /// </summary>
        private void OnEnable()
        {
            _inputActions.Enable();
        }

        /// <summary>
        /// 禁用控制器
        /// </summary>
        private void OnDisable()
        {
            _inputActions.Disable();
        }

        /// <summary>
        /// 销毁时清理资源
        /// </summary>
        private void OnDestroy()
        {
            // 取消订阅模型事件并清理模型
            if (m_model != null)
            {
                m_model.OnInventoryChanged -= UpdateInventoryView;
                m_model.Cleanup();
            }

            // 解绑视图
            m_view?.UnbindController();
        }

        /// <summary>
        /// 初始化MVC组件：创建模型、订阅事件、添加测试物品
        /// View 由 InventoryView.TryBindController 侧绑定注入，此处不主动查找
        /// </summary>
        private void InitializeMVCComponents()
        {
            // 创建并初始化Model
            CreateAndInitializeModel();

            // 订阅Model变更事件
            m_model.OnInventoryChanged += UpdateInventoryView;

            // 测试添加物品（此时View尚未注入，UpdateInventoryView内判空跳过）
            AddTestItems();
        }

        /// <summary>
        /// 注入视图引用并完成视图初始化
        /// View侧绑定模式下由 InventoryView.TryBindController 调用
        /// </summary>
        /// <param name="view">背包视图</param>
        public override void SetView(InventoryView view)
        {
            base.SetView(view);

            // 视图注入后初始化槽位并刷新一次显示（测试物品已加入模型）
            if (m_view != null)
            {
                m_view.InitializeInventory(m_model.Capacity);
                UpdateInventoryView();
            }
        }

        /// <summary>
        /// 更新函数，处理输入检测
        /// </summary>
        private void Update()
        {
            // 使用InputSystem检测快捷键
            if (_inputActions.GamePlay.Inventory.triggered)
            {
                GameEvents.TriggerMenuShow(UIType.Inventory, true);
            }
        }

        #endregion

        #region 测试数据

        /// <summary>
        /// 添加测试物品
        /// </summary>
        private void AddTestItems()
        {
            // 添加测试物品
            AddItem("health_potion", 5);
            AddItem("sword", 1);
            AddItem("gold_coin", 42);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加物品
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <param name="quantity">数量，默认为1</param>
        /// <returns>是否成功添加</returns>
        public bool AddItem(string itemID, int quantity = 1)
        {
            ItemData item = itemDatabase.GetItem(itemID);
            if (item == null) return false;

            return m_model.AddItem(item, quantity);
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <param name="quantity">数量，默认为1</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveItem(string itemID, int quantity = 1)
        {
            return m_model.RemoveItem(itemID, quantity);
        }

        /// <summary>
        /// 移动物品
        /// </summary>
        /// <param name="fromIndex">源物品索引</param>
        /// <param name="toIndex">目标物品索引</param>
        public void MoveItem(int fromIndex, int toIndex)
        {
            m_model.MoveItem(fromIndex, toIndex);
        }

        /// <summary>
        /// 使用物品（按槽位索引）
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        public void UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= m_model.Items.Count) return;

            var item = itemDatabase.GetItem(m_model.Items[slotIndex].ItemID);
            if (item != null && item.Type == ItemData.ItemType.Consumable)
            {
                // 实际使用逻辑
                Log.Info(LogModules.INVENTORY, $"使用物品: {item.Name}");

                // 移除一个物品
                RemoveItem(item.ID, 1);
            }
        }

        /// <summary>
        /// 使用物品（按物品数据）
        /// </summary>
        /// <param name="currentItem">要使用的物品数据</param>
        internal void UseItem(ItemData currentItem)
        {
            if (currentItem != null && currentItem.Type == ItemData.ItemType.Consumable)
            {
                // 实际使用逻辑
                Log.Info(LogModules.INVENTORY, $"使用物品: {currentItem.Name}");

                // 移除一个物品
                RemoveItem(currentItem.ID, 1);
            }
        }

        /// <summary>
        /// 显示物品详情
        /// </summary>
        /// <param name="item">物品数据</param>
        public void ShowItemDetails(ItemData item)
        {
            // 实际项目中这里会显示一个详情面板
            Log.Info(LogModules.INVENTORY, $"显示物品详情: {item.Name}\n{item.Description}");
        }

        #endregion

        #region View更新

        /// <summary>
        /// 更新背包视图
        /// </summary>
        private void UpdateInventoryView()
        {
            // View尚未注入时跳过（AddTestItems在Awake阶段触发的事件）
            if (m_view == null) return;

            // 更新所有槽位
            for (int i = 0; i < m_model.Capacity; i++)
            {
                if (i < m_model.Items.Count)
                {
                    var item = itemDatabase.GetItem(m_model.Items[i].ItemID);
                    m_view.UpdateSlot(i, item, m_model.Items[i].Quantity);
                }
                else
                {
                    m_view.UpdateSlot(i, null, 0);
                }
            }

            // 更新容量显示
            m_view.UpdateCapacity(m_model.Items.Count, m_model.Capacity);
        }

        #endregion
    }
}
