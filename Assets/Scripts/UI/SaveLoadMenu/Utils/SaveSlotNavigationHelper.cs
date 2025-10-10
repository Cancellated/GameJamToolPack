using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using MyGame.UI.SaveLoad.View;
using UnityEngine.EventSystems;
using MyGame.Data;
using System;

namespace MyGame.UI.SaveLoad.Utils
{
    /// <summary>
    /// 保存槽导航辅助类
    /// 处理存档槽的选择、导航和滚动逻辑
    /// </summary>
    public class SaveSlotNavigationHelper
    {
        private ScrollRect _scrollRect;
        private List<ISaveSlotUI> _saveSlotUIs;
        private int _currentSelectedIndex = -1;
        private SaveLoadMenuView _view;
        private Action<int> _onSlotSelected;

        /// <summary>
        /// 存档槽选中事件回调
        /// </summary>
        public Action<int> OnSlotSelected
        {
            set { _onSlotSelected = value; }
        }

        /// <summary>
        /// 初始化导航辅助类
        /// </summary>
        /// <param name="scrollRect">滚动区域组件</param>
        /// <param name="view">SaveLoadMenuView引用</param>
        public void Initialize(ScrollRect scrollRect, SaveLoadMenuView view)
        {
            _scrollRect = scrollRect;
            _view = view;
            _saveSlotUIs = new List<ISaveSlotUI>();
            _currentSelectedIndex = -1;
            _onSlotSelected = null;
        }

        /// <summary>
        /// 设置滚动和遮罩组件
        /// </summary>
        /// <param name="container">容器变换</param>
        public void SetupScrollAndMask(Transform container)
        {
            if (_scrollRect == null) return;

            // 确保滚动区域设置正确
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.vertical = true;
            _scrollRect.horizontal = false;

            // 如果有容器，确保其位置正确
            if (container != null)
            {
                container.localPosition = Vector3.zero;
                container.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 初始化选中状态
        /// </summary>
        /// <param name="saveSlotUIs">存档槽UI列表</param>
        /// <param name="model">数据模型</param>
        public void InitializeSelection(List<ISaveSlotUI> saveSlotUIs)
        {
            _saveSlotUIs = saveSlotUIs;
            
            // 如果有存档槽，默认选中第一个
            if (_saveSlotUIs != null && _saveSlotUIs.Count > 0)
            {
                SelectAndScrollToSlot(0);
            }
        }
        
        /// <summary>
        /// 更新选择UI状态
        /// 用于在不重新创建存档槽UI的情况下更新选中状态
        /// </summary>
        public void UpdateSelectionUI()
        {
            if (_saveSlotUIs == null || _saveSlotUIs.Count == 0)
                return;
            
            // 更新所有存档槽的选择状态
            for (int i = 0; i < _saveSlotUIs.Count; i++)
            {
                if (_saveSlotUIs[i] != null)
                {
                    _saveSlotUIs[i].SetSelected(i == _currentSelectedIndex);
                }
            }
            
            // 如果有选中的存档槽，确保它在视口中可见
            if (_currentSelectedIndex >= 0 && _currentSelectedIndex < _saveSlotUIs.Count)
            {
                ScrollToSlot(_currentSelectedIndex);
            }
        }

        /// <summary>
        /// 重置选择状态
        /// </summary>
        public void ResetSelection()
        {
            _currentSelectedIndex = -1;
            // 清除所有选中状态
            if (_saveSlotUIs != null)
            {
                foreach (var slotUI in _saveSlotUIs)
                {
                    slotUI.SetSelected(false);
                }
            }
        }

        /// <summary>
        /// 处理键盘导航逻辑
        /// </summary>
        public void HandleKeyboardNavigation()
        {
            if (_saveSlotUIs == null || _saveSlotUIs.Count == 0)
                return;

            // 向上导航
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                _currentSelectedIndex = (_currentSelectedIndex - 1 + _saveSlotUIs.Count) % _saveSlotUIs.Count;
                SelectAndScrollToSlot(_currentSelectedIndex);
            }

            // 向下导航
            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                _currentSelectedIndex = (_currentSelectedIndex + 1) % _saveSlotUIs.Count;
                SelectAndScrollToSlot(_currentSelectedIndex);
            }
        }

        /// <summary>
        /// 选择并滚动到指定索引的存档槽
        /// </summary>
        /// <param name="index">存档槽索引</param>
        public void SelectAndScrollToSlot(int index)
        {
            if (index < 0 || index >= _saveSlotUIs.Count)
                return;

            _currentSelectedIndex = index;
            ScrollToSlot(index);
            _onSlotSelected?.Invoke(index);
        }

        /// <summary>
        /// 滚动到指定索引的存档槽
        /// </summary>
        /// <param name="index">存档槽索引</param>
        public void ScrollToSlot(int index)
        {
            if (_scrollRect == null || index < 0 || index >= _saveSlotUIs.Count)
                return;

            var slotUI = _saveSlotUIs[index];
            if (slotUI is MonoBehaviour monoBehaviour)
            {
                RectTransform slotRect = monoBehaviour.GetComponent<RectTransform>();
                RectTransform contentRect = _scrollRect.content;
                RectTransform viewportRect = _scrollRect.viewport;

                if (slotRect != null && contentRect != null && viewportRect != null)
                {
                    // 计算滚动位置
                    float contentHeight = contentRect.rect.height;
                    float viewportHeight = viewportRect.rect.height;
                    float slotHeight = slotRect.rect.height;
                    float slotPosition = slotRect.anchoredPosition.y;

                    // 计算需要的滚动位置，确保选中的存档槽在视口中居中显示
                    float centerOffset = (viewportHeight - slotHeight) / 2;
                    float targetPosition = -slotPosition - centerOffset;
                    float normalizedPosition = 1 - Mathf.Clamp(targetPosition, 0, contentHeight - viewportHeight) / (contentHeight - viewportHeight);

                    // 平滑滚动到目标位置
                    if (_view is MonoBehaviour viewMono)
                    {
                        viewMono.StartCoroutine(SmoothScrollTo(normalizedPosition, 0.2f));
                    }
                }
            }
        }

        /// <summary>
        /// 平滑滚动到指定的标准化位置
        /// </summary>
        /// <param name="targetPosition">目标标准化位置</param>
        /// <param name="duration">滚动持续时间</param>
        private IEnumerator SmoothScrollTo(float targetPosition, float duration)
        {
            float startPosition = _scrollRect.verticalNormalizedPosition;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
                _scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            _scrollRect.verticalNormalizedPosition = targetPosition;
        }
    }
}
