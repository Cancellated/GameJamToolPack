using UnityEditor.EditorTools;
using UnityEngine;

namespace MyGame.UI.SaveLoad
{
    /// <summary>
    /// 存档菜单配置类
    /// 用于定义存档菜单的各种配置项
    /// </summary>
    [CreateAssetMenu(fileName = "SaveLoadMenuConfig", menuName = "UI/SaveLoadMenuConfig")]
    public class SaveLoadMenuConfig : ScriptableObject
    {
        [Header("存档配置")]
        [Tooltip("是否启用自动存档")]
        public bool EnableAutoSave = true;

        [Tooltip("自动存档间隔（秒）")]
        public float AutoSaveInterval = 300f; // 5分钟
        
        [Tooltip("自动存档槽名称")]
        public string AutoSaveSlotName = "AutoSave";
        
        [Tooltip("默认存档槽数量")]
        public int DefaultSaveSlotCount = 30;
        [Tooltip("每页显示的存档槽数量")]
        public int SlotsPerPage = 4;

        [Header("UI配置")]
        [Tooltip("存档槽预制件的Addressable Address")]
        public string SaveSlotPrefabAddress = "SaveSlotPrefab";

        [Header("文本配置")]
        [Tooltip("保存存档确认文本")]
        public string SaveConfirmText = "确定要保存当前游戏吗？";
        
        [Tooltip("删除存档确认文本")]
        public string DeleteConfirmText = "确定要删除该存档吗？此操作不可恢复。";
        
        [Tooltip("加载存档确认文本")]
        public string LoadConfirmText = "确定要加载该存档吗？这将覆盖当前进度。";

        [Tooltip("创建新游戏确认文本")]
        public string CreateNewGameConfirmText = "确定要创建新游戏吗？";
        
        [Header("音效配置")]
        [Tooltip("选择存档槽音效")]
        public AudioClip SelectSlotSound = null;
        
        [Tooltip("保存游戏音效")]
        public AudioClip SaveGameSound = null;
        
        [Tooltip("加载游戏音效")]
        public AudioClip LoadGameSound = null;
        
        [Tooltip("创建新游戏音效")]
        public AudioClip CreateNewGameSound = null;
        
        [Tooltip("删除存档音效")]
        public AudioClip DeleteSaveSound = null;
        
    }
}