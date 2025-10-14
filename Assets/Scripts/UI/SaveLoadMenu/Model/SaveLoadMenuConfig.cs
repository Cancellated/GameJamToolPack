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
        [Header("存档槽配置")]
        [Tooltip("单页存档槽数量")]
        public int SlotsPerPage = 4;
        [Tooltip("最大自动存档数量")]
        public int MaxAutoSaveCount = 2;
        
        [Tooltip("最大手动存档数量")]
        public int MaxManualSaveCount = 36;
        
        [Tooltip("自动存档槽名称")]
        public string AutoSaveSlotName = "AutoSave";
        
        [Header("UI配置")]
        [Tooltip("存档槽预制件的Addressable Address")]
        public string SaveSlotPrefabAddress = "Assets/Art Asset/Prefabs/UI/SaveLoad/Slot.prefab";
        
        [Header("文本配置")]
        [Tooltip("新建游戏确认文本")]
        public string NewGameConfirmText = "确定要创建新游戏吗？";

        [Tooltip("保存存档确认文本")]
        public string SaveConfirmText = "确定要保存到该存档吗？";
        
        [Tooltip("删除存档确认文本")]
        public string DeleteSaveConfirmText = "确定要删除该存档吗？此操作不可恢复。";
        
        [Tooltip("加载存档确认文本")]
        public string LoadSaveConfirmText = "确定要加载该存档吗？这将覆盖当前进度。";
        
        [Header("音效配置")]
        [Tooltip("选择存档槽音效")]
        public AudioClip SelectSlotSound = null;
        
        [Tooltip("保存游戏音效")]
        public AudioClip SaveGameSound = null;
        
        [Tooltip("加载游戏音效")]
        public AudioClip LoadGameSound = null;
        
        [Tooltip("删除游戏音效")]
        public AudioClip DeleteGameSound = null;
        
        [Tooltip("创建新游戏音效")]
        public AudioClip CreateNewGameSound = null;
    }
}