using UnityEngine;
using MyGame.Events;
using MyGame.UI;
using MyGame.UI.Inventory.Data;
using MyGame.UI.Inventory.Model;
using MyGame.UI.Inventory.View;
using Logger;

namespace MyGame.UI.Inventory.Controller
{
    /// <summary>
    /// 背包控制器类，负责处理背包的逻辑和数据管理
    /// 作为MVC架构中的控制器层，继承BaseController以遵循MVC规范
    ///
    /// 【初始化契约】（标准模式）
    ///   - 由 InventoryView.TryBindController 调用 Initialize() + SetView()；
    ///   - Model 在 Initialize() 中创建；
    ///   - 输入监听由 UIController 统一处理（本类不再自行监听 Inventory 键，避免双输入源）。
    /// </summary>
    public class InventoryController : BaseController<InventoryView, InventoryModel>
    {
        #region 字段

        [Tooltip("物品数据库")]
        [SerializeField] private ItemDatabase itemDatabase;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化控制器（由 InventoryView.TryBindController 调用）。
        /// 创建 Model 并订阅 Model 变更事件，再触发基类 OnInitialize。
        /// </summary>
        public override void Initialize()
        {
            if (!IsInitialized)
            {
                // 创建并初始化Model
                CreateAndInitializeModel();

                // 订阅Model变更事件
                m_model.OnInventoryChanged += UpdateInventoryView;

                // 调用基类初始化（触发 OnInitialize）
                base.Initialize();
            }
        }

        /// <summary>
        /// 注入视图引用并完成视图初始化
        /// View侧绑定模式下由 InventoryView.TryBindController 调用
        /// </summary>
        /// <param name="view">背包视图</param>
        public override void SetView(InventoryView view)
        {
            base.SetView(view);

            // 视图注入后初始化槽位并刷新一次显示
            if (m_view != null && m_model != null)
            {
                m_view.InitializeInventory(m_model.Capacity);
                UpdateInventoryView();
            }
        }

        /// <summary>
        /// 清理控制器资源（解绑 Model 事件、清理模型）
        /// </summary>
        public override void Cleanup()
        {
            if (IsInitialized)
            {
                // 取消订阅模型事件
                if (m_model != null)
                {
                    m_model.OnInventoryChanged -= UpdateInventoryView;
                    m_model.Cleanup();
                    m_model = null;
                }

                base.Cleanup();
            }
        }

        /// <summary>
        /// 销毁时清理资源（兜底：确保 Model 事件解绑与视图解绑）
        /// </summary>
        private void OnDestroy()
        {
            Cleanup();

            // 解绑视图
            m_view?.UnbindController();
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
            // View尚未注入时跳过
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
