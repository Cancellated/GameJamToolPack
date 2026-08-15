using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MyGame.UI.Inventory.Data;
using MyGame.UI.Inventory.Controller;
using MyGame.UI;
using System.Collections.Generic;
using Logger;

namespace MyGame.UI.Inventory.View
{
    /// <summary>
    /// 背包视图类，负责背包UI的显示和交互
    /// </summary>
    public class InventoryView : BaseView<InventoryController>
    {
        [Header("UI References")]
        [Tooltip("背包面板")]
        [SerializeField] private GameObject inventoryPanel;
        [Tooltip("物品槽容器")]
        [SerializeField] private Transform itemsContainer;
        [Tooltip("物品槽预制体")]
        [SerializeField] private InventorySlot slotPrefab;
        [Tooltip("容量显示文本")]
        [SerializeField] private TMP_Text capacityText;
        [Tooltip("关闭按钮")]
        [SerializeField] private Button closeButton;
        
        [Header("Drag & Drop")]
        [Tooltip("拖拽Canvas")]
        [SerializeField] private Canvas dragCanvas;
        [Tooltip("拖拽图标")]
        [SerializeField] private Image dragIcon;
        
        private InventorySlot[] slots;
        private bool isDragging;
        private int draggedSlotIndex = -1;
        private int capacity;
        
        /// <summary>
        /// 初始化（仅设置面板类型与基础绑定）
        /// </summary>
        protected override void Awake()
        {
            // 设置面板类型
            m_panelType = UIType.Inventory;
            
            // 调用基类初始化
            base.Awake();
            
            dragIcon.gameObject.SetActive(false);
        }

        /// <summary>
        /// 初始化面板：绑定按钮事件（遵循基类规范：在 Initialize 中绑定，与 Cleanup 中的解绑成对）
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // 绑定关闭按钮事件
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
        }

        /// <summary>
        /// 尝试自动绑定控制器（标准模式：同物体查找，初始化并注入视图）
        /// </summary>
        protected override void TryBindController()
        {
            if (TryGetComponent<InventoryController>(out var controller))
            {
                // 找到控制器：初始化、注入视图并绑定
                controller.Initialize();
                controller.SetView(this);
                BindController(controller);
                return;
            }

            // 如果没有找到控制器，则创建一个新的
            controller = gameObject.AddComponent<InventoryController>();
            controller.Initialize();
            controller.SetView(this);

            // 绑定控制器到视图
            BindController(controller);
        }
        
        /// <summary>
        /// 初始化背包UI
        /// </summary>
        public void InitializeInventory(int newCapacity)
        {
            capacity = newCapacity;
            
            // 清空现有槽位
            foreach (Transform child in itemsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 创建新槽位
            slots = new InventorySlot[capacity];
            for (int i = 0; i < capacity; i++)
            {
                slots[i] = Instantiate(slotPrefab, itemsContainer);
                slots[i].Initialize(i, this);
            }
            
            capacityText.text = $"0/{capacity}";
        }
        
        /// <summary>
        /// 更新槽位显示
        /// </summary>
        public void UpdateSlot(int index, ItemData item, int quantity)
        {
            if (index < 0 || index >= slots.Length) return;
            
            slots[index].SetItem(item, quantity);
        }
        
        /// <summary>
        /// 更新容量显示
        /// </summary>
        public void UpdateCapacity(int current, int max)
        {
            capacityText.text = $"{current}/{max}";
        }
        
        /// <summary>
        /// 显示面板（重写基类方法）
        /// </summary>
        public override void Show()
        {
            base.Show();
            Log.Info(LogModules.INVENTORY, "背包已显示");
        }
        
        /// <summary>
        /// 隐藏面板（重写基类方法）
        /// </summary>
        public override void Hide()
        {
            base.Hide();
            Log.Info(LogModules.INVENTORY, "背包已隐藏");
        }

        /// <summary>
        /// 清理面板资源
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();

            // 解绑关闭按钮事件，与 Awake 中的绑定成对出现
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }
        }
        
        /// <summary>
        /// 开始拖拽物品
        /// </summary>
        public void StartDrag(int slotIndex, Sprite icon)
        {
            isDragging = true;
            draggedSlotIndex = slotIndex;
            dragIcon.sprite = icon;
            dragIcon.gameObject.SetActive(true);
            dragIcon.transform.position = Input.mousePosition;
        }
        
        /// <summary>
        /// 更新拖拽位置
        /// </summary>
        private void Update()
        {
            if (isDragging)
            {
                dragIcon.transform.position = Input.mousePosition;
            }
        }
        
        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void EndDrag(int targetSlotIndex)
        {
            if (!isDragging) return;
            
            dragIcon.gameObject.SetActive(false);
            isDragging = false;
            
            if (targetSlotIndex >= 0 && targetSlotIndex < slots.Length)
            {
                if(m_controller != null)
                {
                    m_controller.MoveItem(draggedSlotIndex, targetSlotIndex);
                }
            }
        }
        
        /// <summary>
        /// 使用物品
        /// </summary>
        /// <param name="item">要使用的物品数据</param>
        public void UseItem(ItemData item)
        {
            if (m_controller != null)
            {
                m_controller.UseItem(item);
            }
        }
    }
}