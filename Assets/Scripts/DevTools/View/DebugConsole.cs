using System;
using UnityEngine;
using TMPro;
using System.Linq;
using MyGame.Managers;
using MyGame.Events;
using Logger;
using MyGame.UI;


namespace MyGame.DevTool
{
    /// <summary>
    /// 调试控制台视图，负责UI展示和用户输入事件捕获
    /// </summary>
    public class DebugConsole : BaseView<DebugConsoleController>
    {
        private const string LOG_MODULE = LogModules.DEBUGCONSOLE;
        //最大日志保留数
        private const int MAX_LOG_LINES = 100;

        private static DebugConsole s_instance;

        /// <summary>
        /// 静态实例，用于全局访问
        /// </summary>
        public static DebugConsole Instance
        {
            get { return s_instance; }
        }
        #region UI引用

        [Header("UI组件引用")]
        [Tooltip("命令输入框")]
        public TMP_InputField inputField;

        [Tooltip("输出文本框")]
        public TMP_Text outputText;

        [Tooltip("滚动视图组件")]
        public UnityEngine.UI.ScrollRect scrollRect;

        [Header("层级设置")]
        [Tooltip("控制台Canvas的Sorting Order。值越高，显示层级越高，不易被其他UI遮挡。")]
        public int canvasSortingOrder = 900; // 设置较高的默认值，确保控制台显示在大多数UI上层
        #endregion

        #region 生命周期函数

        /// <summary>
        /// 初始化组件（仅注册静态实例与设置排序层级；输入框绑定见 Initialize）
        /// </summary>
        protected override void Awake()
        {
            // 设置面板类型为Console
            m_panelType = UIType.Console;
            
            base.Awake();

            // 设置静态实例
            s_instance = this;

            // 设置Canvas排序层级
            SetCanvasSortingOrder();
        }

        /// <summary>
        /// 当对象被销毁时清理静态实例引用，防止悬空
        /// </summary>
        protected override void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
            base.OnDestroy();
        }

        /// <summary>
        /// 尝试自动绑定控制器（标准模式：同物体查找，初始化并注入视图）。
        /// </summary>
        protected override void TryBindController()
        {
            if (TryGetComponent<DebugConsoleController>(out var controller))
            {
                // 找到预挂的控制器：初始化、注入视图并绑定
                controller.Initialize();
                controller.SetView(this);
                BindController(controller);
                return;
            }

            // 未预挂时创建
            controller = gameObject.AddComponent<DebugConsoleController>();
            controller.Initialize();
            controller.SetView(this);

            // 绑定控制器到视图
            BindController(controller);
        }

        /// <summary>
        /// 设置Canvas的Sorting Order，确保控制台显示在其他UI上层
        /// </summary>
        private void SetCanvasSortingOrder()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = canvasSortingOrder;
            }
            else
            {
                Log.Warning(LOG_MODULE, "未找到父级Canvas组件，无法设置排序层级。", this);
            }
        }

        /// <summary>
        /// 初始化面板：绑定输入框事件（遵循基类规范：在 Initialize 中绑定，与 Cleanup 中的解绑成对）
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            if (outputText != null)
                outputText.text = "调试控制台已启动。输入 help 查看命令。";

            // 绑定输入框事件处理器
            if (inputField != null)
            {
                // 使用 onSubmit 而非 onEndEdit：项目使用 InputSystemUIInputModule，
                // 按回车提交时走 submitHandler -> TMP_InputField.OnSubmit -> onSubmit，
                // 而不会触发 onEndEdit（输入框不会自动失焦）。
                inputField.onSubmit.AddListener(HandleInputSubmit);
                // 设置输入行为模式为提交时结束编辑
                inputField.lineType = TMP_InputField.LineType.SingleLine;
            }

            Log.Info(LOG_MODULE, "DebugConsole.PanelType 设置为: " + m_panelType);
        }

        /// <summary>
        /// 清理面板资源：解绑输入框事件
        /// </summary>
        public override void Cleanup()
        {
            // 解绑输入框事件（与 Initialize 中的绑定成对出现）
            if (inputField != null)
            {
                inputField.onSubmit.RemoveListener(HandleInputSubmit);
            }

            base.Cleanup();
        }

        #endregion

        #region 公共方法
        // 重写Show方法-控制台不需要动画控制显隐
        public override void Show()
        {
            Log.Info(LOG_MODULE, "显示调试控制台", this);

            // 确保游戏对象被激活
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                Log.Info(LOG_MODULE, "游戏对象已被激活", this);
            }

            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 1f;
                m_canvasGroup.interactable = true;
                m_canvasGroup.blocksRaycasts = true;
            }

            IsVisible = true;
        }

        // 重写BaseUI的Hide方法
        public override void Hide()
        {
            Log.Info(LOG_MODULE, "隐藏调试控制台", this);

            // 确保游戏对象被禁用
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 0f;
                m_canvasGroup.interactable = false;
                m_canvasGroup.blocksRaycasts = false;
            }

            IsVisible = false;
        }

        /// <summary>
        /// 输入框提交事件处理（TMP_InputField.onSubmit 回调，命名方法以便与 Cleanup 解绑成对）
        /// </summary>
        /// <param name="input">输入框的当前文本内容</param>
        private void HandleInputSubmit(string input)
        {
            OnCommandEntered(input);
        }

        /// <summary>
        /// 当输入命令时调用
        /// </summary>
        /// <param name="input">输入框的当前文本内容（TMP_InputField的onSubmit/onEndEdit事件会传入）；为null时回退到输入框当前文本</param>
        public void OnCommandEntered(string input = null)
        {
            // 获取输入文本：优先使用事件参数，外部直接调用时回退到输入框当前文本
            string cmd = (input ?? inputField.text).Trim().ToLower();

            // 空命令不执行，仅重新激活输入框以便继续输入
            if (string.IsNullOrEmpty(cmd))
            {
                inputField.ActivateInputField();
                return;
            }

            // 清除输入框
            inputField.text = "";

            // 将命令转发给控制器处理
            if (m_controller != null)
            {
                m_controller.HandleCommand(cmd);
            }
            else
            {
                Log.Warning(LOG_MODULE, "控制器未绑定，无法执行命令: " + cmd, this);
            }

            // 重新激活输入框以便继续输入
            inputField.ActivateInputField();
        }

        /// <summary>
        /// 在控制台中显示文本消息
        /// </summary>
        /// <param name="msg">要显示的消息</param>
        public void DisplayText(string msg)
        {
            if (outputText != null)
            {
                // 分割现有日志为行数组
                var lines = outputText.text.Split('\n');


                // 如果超过最大行数，移除最早的行
                if (lines.Length >= MAX_LOG_LINES)
                {
                    lines = lines.Skip(1).ToArray();
                }

                // 添加新日志并重新组合
                outputText.text = string.Join("\n", lines) + $"\n{msg}";

                // 强制布局更新
                Canvas.ForceUpdateCanvases();

                // 当Scroll Rect存在且内容需要滚动时执行滚动到底部
                if (scrollRect != null && outputText.preferredHeight > outputText.rectTransform.rect.height)
                {
                    scrollRect.verticalNormalizedPosition = 0f; // 0f 表示滚动到底部
                }
            }
            Log.Info(LOG_MODULE, msg, this);
        }

        /// <summary>
        /// 在控制台中显示文本消息（DisplayText的别名方法）
        /// </summary>
        /// <param name="msg">要显示的消息</param>
        public void Print(string msg)
        {
            DisplayText(msg);
        }
        #endregion
    }

    /// <summary>
    /// 调试命令特性，用于标记命令方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DebugCommand : Attribute
    {
        /// <summary>命令名称</summary>
        public string CommandName { get; }
        
        /// <summary>命令描述</summary> 
        public string Description { get; set; }
        
        /// <summary>
        /// 创建调试命令特性
        /// </summary>
        /// <param name="name">命令名称</param>
        public DebugCommand(string name)
        {
            CommandName = name;
        }
    }
}