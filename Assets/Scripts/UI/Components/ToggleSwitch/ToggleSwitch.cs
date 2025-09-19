using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace MyGame.UI.Components
{
    /// <summary>
    /// 独立的自定义左右型开关组件，不依赖Unity的Toggle组件
    /// </summary>
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("UI/ToggleSwitch")]
    public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        #region UI组件
        [Header("开关样式配置")]
        [Tooltip("开关滑块的RectTransform")]
        [SerializeField] private RectTransform m_knobRectTransform;
        
        [Tooltip("开关打开时的背景颜色")]
        [SerializeField] private Color m_onColor = new(0.44f, 0.86f, 0.44f);
        
        [Tooltip("开关关闭时的背景颜色")]
        [SerializeField] private Color m_offColor = new(0.78f, 0.78f, 0.78f);
        
        [Tooltip("开关滑块的移动速度")]
        [SerializeField] private float m_animationSpeed = 0.15f;
        
        [Tooltip("开关打开时的文本")]
        [SerializeField] private string m_onText = "开";
        
        [Tooltip("开关关闭时的文本")]
        [SerializeField] private string m_offText = "关";
        
        [Tooltip("开关文本组件")]
        [SerializeField] private Text m_statusText;
        #endregion

        #region 字段
        // 开关状态
        [Tooltip("开关的当前状态")]
        [SerializeField] private bool m_isOn = false;
        
        private Image m_backgroundImage;
        private Vector2 m_knobStartPosition;
        private Vector2 m_knobEndPosition;
        private bool m_isAnimating = false;
        private float m_animStartTime;
        
        // 定义值变化事件
        public event Action<bool> onValueChanged;
        #endregion

        #region 属性
        /// <summary>
        /// 获取或设置开关状态
        /// </summary>
        public bool isOn
        {
            get { return m_isOn; }
            set
            {
                if (m_isOn != value)
                {
                    m_isOn = value;
                    UpdateSwitchVisuals(m_isOn, false);
                    
                    // 触发值变化事件
                    onValueChanged?.Invoke(m_isOn);
                }
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 初始化组件
        /// </summary>
        private void Start()
        {
            // 获取背景图片组件
            m_backgroundImage = GetComponent<Image>();
            
            // 初始化滑块位置
            if (m_knobRectTransform != null)
            {
                m_knobStartPosition = m_knobRectTransform.anchoredPosition;
                m_knobEndPosition = new Vector2(
                    m_backgroundImage.rectTransform.rect.width - m_knobRectTransform.rect.width,
                    m_knobStartPosition.y
                );
            }
            
            // 初始化开关状态
            UpdateSwitchVisuals(m_isOn, true);
        }

        /// <summary>
        /// 处理点击事件
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 切换开关状态
            isOn = !isOn;
        }

        /// <summary>
        /// 更新开关的视觉效果
        /// </summary>
        /// <param name="isOnState">开关状态</param>
        /// <param name="instant">是否立即更新（无动画）</param>
        private void UpdateSwitchVisuals(bool isOnState, bool instant)
        {
            if (m_backgroundImage == null)
                return;

            // 更新背景颜色
            m_backgroundImage.color = isOnState ? m_onColor : m_offColor;
            
            // 更新文本（如果有）
            if (m_statusText != null)
            {
                m_statusText.text = isOnState ? m_onText : m_offText;
            }
            
            // 更新滑块位置
            if (m_knobRectTransform != null)
            {
                if (instant || m_animationSpeed <= 0)
                {
                    m_knobRectTransform.anchoredPosition = isOnState ? m_knobEndPosition : m_knobStartPosition;
                    m_isAnimating = false;
                }
                else
                {
                    m_animStartTime = Time.time;
                    m_isAnimating = true;
                }
            }
        }

        /// <summary>
        /// 每帧更新，处理开关动画
        /// </summary>
        private void Update()
        {
            if (m_isAnimating && m_knobRectTransform != null)
            {
                float elapsed = Time.time - m_animStartTime;
                float t = Mathf.Clamp01(elapsed / m_animationSpeed);
                
                // 使用平滑的插值函数
                t = m_isOn ? SmoothStep(0, 1, t) : SmoothStep(1, 0, t);
                
                // 更新滑块位置
                m_knobRectTransform.anchoredPosition = Vector2.Lerp(
                    m_knobStartPosition, 
                    m_knobEndPosition, 
                    t
                );
                
                // 动画完成
                if (t >= 1 || t <= 0)
                {
                    m_isAnimating = false;
                }
            }
        }

        /// <summary>
        /// 平滑步进函数，用于创建更自然的动画效果
        /// </summary>
        private float SmoothStep(float start, float end, float t)
        {
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(start, end, t);
        }

        /// <summary>
        /// 在编辑器模式下验证组件设置
        /// </summary>
        private void OnValidate()
        {
            // 在编辑器中立即更新视觉效果
            if (Application.isPlaying)
            {
                UpdateSwitchVisuals(m_isOn, true);
            }
        }
        #endregion
    }
}