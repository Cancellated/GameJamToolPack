using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.UI.SaveLoadMenu.View.Components
{
    public interface IConfirm
    {
        /// <summary>
        /// 显示确认弹窗
        /// </summary>
        /// <param name="state">确认弹窗状态</param>
        /// <param name="onConfirm">确认回调</param>
        /// <param name="onCancel">取消回调</param>
        void GetConfirmMessage(ConfirmDialogState state);
        void ShowConfirmDialog(ConfirmDialogState state, System.Action onConfirm, System.Action onCancel = null);
        void HideConfirmDialog();
    }
}
