using System;
using UnityEngine;
using Logger;
using MyGame.DevTools;
using MyGame.Events;
using MyGame.UI;
using MyGame.UI.Settings.Model;
using MyGame.UI.Settings.View;
using System.Collections.Generic;

namespace MyGame.UI.Settings.Controller
{
    /// <summary>
    /// 设置面板控制器
    /// 负责处理设置面板的逻辑和设置更新
    /// </summary>
    public class SettingsPanelController : BaseController<SettingsPanelView, SettingsModel>, ISettingsValueProvider
    {
        #region 字段

        [Header("Settings Panel View")]
        [Tooltip("设置面板视图引用")]
        [SerializeField] private SettingsPanelView m_settingsPanelView;

        private const string LOG_MODULE = LogModules.SETTINGS + "Controller";

        /// <summary>选项 key -> 读取当前值</summary>
        private Dictionary<string, Func<object>> m_optionGetters;

        /// <summary>选项 key -> 写入新值</summary>
        private Dictionary<string, Action<object>> m_optionSetters;

        #endregion

        #region 生命周期方法

        /// <summary>
        /// 初始化控制器（由 SettingsPanelView.TryBindController 调用）。
        /// 创建 Model、注入 Inspector 配置的视图引用（View 侧注入优先）。
        /// </summary>
        public override void Initialize()
        {
            if (!IsInitialized)
            {
                Log.Info(LOG_MODULE, "初始化设置面板控制器");

                // 创建并初始化模型
                CreateAndInitializeModel();

                // 初始化标准化设置项注册表（供 SettingsOptionRowView 经 ISettingsValueProvider 按 key 读写）
                InitializeOptionRegistry();

                // 视图注入兜底：View 侧 TryBindController 未注入时使用 Inspector 引用
                if (m_view == null && m_settingsPanelView != null)
                {
                    SetView(m_settingsPanelView);
                }

                // 调用基类初始化（触发 OnInitialize）
                base.Initialize();
            }
        }

        /// <summary>
        /// 初始化逻辑：订阅 Model 变更事件
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            BindModelEvents();
        }

        /// <summary>
        /// 清理控制器资源（解绑 Model 事件、清理模型）
        /// </summary>
        public override void Cleanup()
        {
            if (IsInitialized)
            {
                Log.Info(LOG_MODULE, "清理设置面板控制器");

                // 取消订阅 Model 事件
                UnbindModelEvents();

                // 清理模型资源
                if (m_model != null)
                {
                    m_model.Cleanup();
                    m_model = null;
                }

                // 调用基类清理
                base.Cleanup();
            }
        }

        #endregion

        #region Model事件绑定

        /// <summary>
        /// 订阅 Model 属性变更事件
        /// </summary>
        private void BindModelEvents()
        {
            Log.Info(LOG_MODULE, "绑定设置模型事件");
            if (m_model != null)
            {
                m_model.OnPropertyChanged += HandleModelPropertyChanged;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，无法绑定事件");
            }
        }

        /// <summary>
        /// 取消订阅 Model 属性变更事件
        /// </summary>
        private void UnbindModelEvents()
        {
            if (m_model != null)
            {
                m_model.OnPropertyChanged -= HandleModelPropertyChanged;
            }
        }

        #endregion

        #region 属性变化处理

        /// <summary>
        /// 处理设置模型属性变化
        /// </summary>
        /// <param name="propertyName">变化的属性名称</param>
        private void HandleModelPropertyChanged(string propertyName)
        {
            Log.InfoWithCooldown(LOG_MODULE, "检测到设置模型属性变化: " + propertyName,
                "settings_model_property_" + propertyName, 1f);

            // 脏状态变化不需要刷新所有设置组件
            if (propertyName == nameof(SettingsModel.HasUnsavedChanges))
            {
                return;
            }

            UpdateViewWithCurrentSettings();
        }

        #endregion

        #region 通用值更新方法

        /// <summary>
        /// 通用的值更新方法，用于统一检查模型是否为空、值是否真正改变，并记录日志
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="currentValue">当前值</param>
        /// <param name="newValue">新值</param>
        /// <param name="isValueChanged">值比较器，用于确定值是否已改变</param>
        /// <param name="updateAction">值更新操作</param>
        /// <param name="logMessage">日志消息</param>
        /// <param name="errorMessage">模型为空时的错误消息</param>
        /// <param name="cooldownKey">日志冷却 key（必须固定，不能拼接动态值，否则冷却失效且缓存字典无限增长）</param>
        /// <param name="debugLogFormat">调试日志格式化函数</param>
        private void UpdateValue<T>(T currentValue, T newValue, Func<T, T, bool> isValueChanged, Action updateAction, string logMessage, string errorMessage, string cooldownKey, Func<T, T, string> debugLogFormat = null)
        {
            if (m_model != null)
            {
                // 检查值是否真正改变
                if (isValueChanged(currentValue, newValue))
                {
                    Log.InfoWithCooldown(LOG_MODULE, logMessage, cooldownKey, 1f);
                    
                    // 如果提供了调试日志格式化函数，则记录调试日志
                    if (debugLogFormat != null)
                    {
                        Log.DebugLog(LOG_MODULE, debugLogFormat(currentValue, newValue));
                    }
                    
                    // 执行更新操作
                    updateAction();
                }
            }
            else
            {
                Log.Error(LOG_MODULE, errorMessage);
            }
        }

        #endregion

        #region 设置更新方法

        /// <summary>
        /// 更新音乐音量设置
        /// </summary>
        /// <param name="volume">新的音量值</param>
        public void UpdateMusicVolume(float volume)
        {
            UpdateValue(
                m_model?.MusicVolume ?? 0f,
                volume,
                (current, newVal) => !Mathf.Approximately(current, newVal),
                () => m_model.MusicVolume = volume,
                "更新音乐音量: " + volume,
                "设置模型为空，无法更新音乐音量",
                "settings_update_musicVolume",
                (current, newVal) => string.Format("设置模型: 更新前音乐音量={0}, 更新后音量={1}", current, newVal)
            );
        }

        /// <summary>
        /// 更新音效音量设置
        /// </summary>
        /// <param name="volume">新的音量值</param>
        public void UpdateSfxVolume(float volume)
        {
            UpdateValue(
                m_model?.SfxVolume ?? 0f,
                volume,
                (current, newVal) => !Mathf.Approximately(current, newVal),
                () => m_model.SfxVolume = volume,
                "更新音效音量: " + volume,
                "设置模型为空，无法更新音效音量",
                "settings_update_sfxVolume",
                (current, newVal) => string.Format("设置模型: 更新前音效音量={0}, 更新后音量={1}", current, newVal)
            );
        }

        /// <summary>
        /// 更新画质级别设置
        /// </summary>
        /// <param name="qualityLevel">新的画质级别</param>
        public void UpdateQualityLevel(int qualityLevel)
        {
            UpdateValue(
                m_model?.QualityLevel ?? 0,
                qualityLevel,
                (current, newVal) => current != newVal,
                () => m_model.QualityLevel = qualityLevel,
                "更新画质级别: " + qualityLevel,
                "设置模型为空，无法更新画质级别",
                "settings_update_qualityLevel",
                (current, newVal) => string.Format("设置模型: 更新前画质级别={0}, 更新后画质级别={1}", current, newVal)
            );
        }

        /// <summary>
        /// 更新全屏状态设置
        /// </summary>
        /// <param name="isFullscreen">是否全屏</param>
        public void UpdateFullscreen(bool isFullscreen)
        {
            UpdateValue(
                m_model?.Fullscreen ?? false,
                isFullscreen,
                (current, newVal) => current != newVal,
                () => m_model.Fullscreen = isFullscreen,
                "更新全屏状态: " + (isFullscreen ? "全屏" : "窗口"),
                "设置模型为空，无法更新全屏状态",
                "settings_update_fullscreen",
                (current, newVal) => string.Format("设置模型: 更新前全屏状态={0}, 更新后全屏状态={1}", 
                    current ? "全屏" : "窗口", newVal ? "全屏" : "窗口")
            );
        }

        /// <summary>
        /// 更新分辨率索引设置
        /// </summary>
        /// <param name="resolutionIndex">新的分辨率索引</param>
        public void UpdateResolutionIndex(int resolutionIndex)
        {
            UpdateValue(
                m_model?.ResolutionIndex ?? 0,
                resolutionIndex,
                (current, newVal) => current != newVal,
                () => m_model.ResolutionIndex = resolutionIndex,
                "更新分辨率索引: " + resolutionIndex,
                "设置模型为空，无法更新分辨率索引",
                "settings_update_resolutionIndex",
                (current, newVal) => string.Format("设置模型: 更新前分辨率索引={0}, 更新后分辨率索引={1}", current, newVal)
            );
        }

        /// <summary>
        /// 更新Y轴反转设置
        /// </summary>
        /// <param name="invertYAxis">是否反转Y轴</param>
        public void UpdateInvertYAxis(bool invertYAxis)
        {
            UpdateValue(
                m_model?.InvertYAxis ?? false,
                invertYAxis,
                (current, newVal) => current != newVal,
                () => m_model.InvertYAxis = invertYAxis,
                "更新Y轴反转设置: " + (invertYAxis ? "反转" : "不反转"),
                "设置模型为空，无法更新Y轴反转设置",
                "settings_update_invertYAxis",
                (current, newVal) => string.Format("设置模型: 更新前Y轴反转={0}, 更新后Y轴反转={1}", 
                    current ? "反转" : "不反转", newVal ? "反转" : "不反转")
            );
        }

        /// <summary>
        /// 更新开发者模式开关：写入 Model 并标记未保存，点击“保存”后持久化生效。
        /// </summary>
        public void SetDeveloperModeEnabled(bool enabled)
        {
            if (m_model == null)
            {
                Log.Error(LOG_MODULE, "设置模型为空，无法更新开发者模式");
                return;
            }

            if (m_model.DeveloperModeEnabled == enabled)
            {
                return;
            }

            m_model.DeveloperModeEnabled = enabled;
            Log.InfoWithCooldown(LOG_MODULE, "更新开发者模式: " + (enabled ? "开启" : "关闭"),
                "settings_developer_mode", 1f);
        }

        #endregion

        #region 设置操作方法

        /// <summary>
        /// 应用当前设置
        /// </summary>
        public bool ApplySettings()
        {
            Log.Info(LOG_MODULE, "应用设置");
            if (m_model != null)
            {
                m_model.ApplySettings();
                Log.DebugLog(LOG_MODULE, "设置已成功应用");
                return true;
            }

            Log.Error(LOG_MODULE, "设置模型为空，无法应用设置");
            return false;
        }

        /// <summary>
        /// 保存当前设置
        /// </summary>
        public bool SaveSettings()
        {
            Log.Info(LOG_MODULE, "保存设置");
            if (m_model != null)
            {
                m_model.SaveSettings();
                Log.DebugLog(LOG_MODULE, "设置已成功保存");
                return true;
            }

            Log.Error(LOG_MODULE, "设置模型为空，无法保存设置");
            return false;
        }

        /// <summary>
        /// 是否存在尚未保存的设置修改。
        /// </summary>
        public bool HasUnsavedChanges()
        {
            return m_model != null && m_model.HasUnsavedChanges;
        }

        /// <summary>
        /// 尝试返回主菜单：有未保存修改时由 View 弹出确认，无修改时直接退出。
        /// </summary>
        public void RequestBackToMainMenu()
        {
            if (HasUnsavedChanges())
            {
                m_view?.ShowUnsavedConfirmDialog();
                return;
            }

            ExitToMainMenu();
        }

        /// <summary>
        /// 放弃未保存修改并返回主菜单。
        /// </summary>
        public void DiscardChangesAndExit()
        {
            m_model?.ReloadFromPlayerPrefs();
            // 如果用户曾点过“应用”，需要把已应用但未保存的修改回滚到已保存值
            m_model?.ApplySettings();
            m_view?.HideUnsavedConfirmDialog();
            ExitToMainMenu();
        }

        /// <summary>
        /// 取消退出，留在设置面板。
        /// </summary>
        public void CancelExit()
        {
            m_view?.HideUnsavedConfirmDialog();
        }

        /// <summary>
        /// 返回主菜单：走 UIManager 统一调度，保证 currentState / 输入模式 / 主菜单交互同步。
        /// </summary>
        private void ExitToMainMenu()
        {
            GameEvents.TriggerMenuShow(UIType.SettingsPanel, false);
            GameEvents.TriggerMenuShow(UIType.MainMenu, true);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新视图以显示当前设置
        /// </summary>
        private void UpdateViewWithCurrentSettings()
        {
            // 优先使用标准注入的 m_view，Inspector 引用作兜底
            SettingsPanelView view = m_view ?? m_settingsPanelView;
            if (view != null)
            {
                view.UpdateAllSettingsComponents();
            }
            else
            {
                Log.Error(LOG_MODULE, "设置面板视图为空，无法更新视图");
            }
        }

        #endregion

        #region 标准化设置项注册表

        /// <summary>
        /// 注册所有可经 Catalog 生成的设置项。
        /// 新增设置项时：在 SettingsCatalog 加定义，并在这里补一对 get/set。
        /// </summary>
        private void InitializeOptionRegistry()
        {
            m_optionGetters = new Dictionary<string, Func<object>>
            {
                ["musicVolume"] = () => m_model != null ? m_model.MusicVolume : 0f,
                ["sfxVolume"] = () => m_model != null ? m_model.SfxVolume : 0f,
                ["qualityLevel"] = () => m_model != null ? m_model.QualityLevel : 0,
                ["fullscreen"] = () => m_model != null ? m_model.Fullscreen : false,
                ["resolutionIndex"] = () => m_model != null ? m_model.ResolutionIndex : 0,
                ["invertYAxis"] = () => m_model != null ? m_model.InvertYAxis : false,
                ["developerMode"] = () => GetDeveloperModeEnabled(),
            };

            m_optionSetters = new Dictionary<string, Action<object>>
            {
                ["musicVolume"] = value => UpdateMusicVolume(Convert.ToSingle(value)),
                ["sfxVolume"] = value => UpdateSfxVolume(Convert.ToSingle(value)),
                ["qualityLevel"] = value => UpdateQualityLevel(Convert.ToInt32(value)),
                ["fullscreen"] = value => UpdateFullscreen(Convert.ToBoolean(value)),
                ["resolutionIndex"] = value => UpdateResolutionIndex(Convert.ToInt32(value)),
                ["invertYAxis"] = value => UpdateInvertYAxis(Convert.ToBoolean(value)),
                ["developerMode"] = value => SetDeveloperModeEnabled(Convert.ToBoolean(value)),
            };
        }

        /// <summary>
        /// 按 key 读取设置值（供 SettingsOptionRowView 回显）。
        /// </summary>
        public object GetOptionValue(string key)
        {
            if (string.IsNullOrEmpty(key) || m_optionGetters == null || !m_optionGetters.TryGetValue(key, out Func<object> getter))
            {
                Log.Warning(LOG_MODULE, $"未注册的设置项 key: {key}");
                return null;
            }

            try
            {
                return getter();
            }
            catch (Exception ex)
            {
                Log.Error(LOG_MODULE, $"读取设置项失败: {key}, {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 按 key 写入设置值（供 SettingsOptionRowView 事件回传）。
        /// </summary>
        public bool SetOptionValue(string key, object value)
        {
            if (string.IsNullOrEmpty(key) || m_optionSetters == null || !m_optionSetters.TryGetValue(key, out Action<object> setter))
            {
                Log.Warning(LOG_MODULE, $"未注册的设置项 key: {key}");
                return false;
            }

            try
            {
                setter(value);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(LOG_MODULE, $"写入设置项失败: {key}, {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ISettingsValueProvider 适配：读取设置值。
        /// </summary>
        public object GetValue(string key)
        {
            return GetOptionValue(key);
        }

        /// <summary>
        /// ISettingsValueProvider 适配：写入设置值。
        /// </summary>
        public void SetValue(string key, object value)
        {
            SetOptionValue(key, value);
        }

        #endregion

        #region 获取设置值方法

        /// <summary>
        /// 获取音乐音量
        /// </summary>
        /// <returns>音乐音量值</returns>
        public float GetMusicVolume()
        {
            if (m_model != null)
            {
                float volume = m_model.MusicVolume;
                Log.DebugLog(LOG_MODULE, "获取音乐音量: " + volume);
                return volume;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，返回默认音乐音量");
                return 1f; // 默认最大音量
            }
        }

        /// <summary>
        /// 获取音效音量
        /// </summary>
        /// <returns>音效音量值</returns>
        public float GetSfxVolume()
        {
            if (m_model != null)
            {
                float volume = m_model.SfxVolume;
                Log.DebugLog(LOG_MODULE, "获取音效音量: " + volume);
                return volume;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，返回默认音效音量");
                return 1f; // 默认最大音量
            }
        }

        /// <summary>
        /// 获取画质级别
        /// </summary>
        /// <returns>画质级别索引</returns>
        public int GetQualityLevel()
        {
            if (m_model != null)
            {
                int qualityLevel = m_model.QualityLevel;
                return qualityLevel;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，返回系统当前画质级别");
                return QualitySettings.GetQualityLevel();
            }
        }

        /// <summary>
        /// 获取全屏状态
        /// </summary>
        /// <returns>是否全屏</returns>
        public bool IsFullscreen()
        {
            if (m_model != null)
            {
                bool isFullscreen = m_model.Fullscreen;
                return isFullscreen;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，返回系统当前全屏状态");
                return Screen.fullScreen;
            }
        }

        /// <summary>
        /// 获取分辨率索引
        /// </summary>
        /// <returns>分辨率索引</returns>
        public int GetResolutionIndex()
        {
            if (m_model != null)
            {
                int resolutionIndex = m_model.ResolutionIndex;
                return resolutionIndex;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，返回默认分辨率索引");
                return 0;
            }
        }

        /// <summary>
        /// 获取Y轴反转状态
        /// </summary>
        public bool GetInvertYAxis()
        {
            if (m_model != null)
            {
                return m_model.InvertYAxis;
            }

            Log.Error(LOG_MODULE, "设置模型为空，返回默认Y轴反转状态");
            return false;
        }

        /// <summary>
        /// 获取开发者模式状态（从 Model 读取，保存流程统一持久化）。
        /// </summary>
        public bool GetDeveloperModeEnabled()
        {
            if (m_model != null)
            {
                return m_model.DeveloperModeEnabled;
            }

            Log.Error(LOG_MODULE, "设置模型为空，返回默认开发者模式状态");
            return false;
        }

        #endregion
    }
}