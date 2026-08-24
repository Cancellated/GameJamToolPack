namespace MyGame.UI.Settings.View
{
    /// <summary>
    /// 设置值提供者接口：解耦“生成 UI”与“读取/写入设置值”。
    /// 运行时由 SettingsPanelController 实现；编辑器预览由预览值提供者实现。
    /// </summary>
    public interface ISettingsValueProvider
    {
        object GetValue(string key);
        void SetValue(string key, object value);
    }
}
