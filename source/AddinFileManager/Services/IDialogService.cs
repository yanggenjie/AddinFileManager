using System;

namespace AddinFileManager.Services;

/// <summary>
/// 对话框服务接口
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// 显示确认对话框
    /// </summary>
    bool ShowConfirm(string message, string title = "确认");

    /// <summary>
    /// 显示消息对话框
    /// </summary>
    void ShowMessage(string message, string title = "提示", MessageType type = MessageType.Info);

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    void ShowError(string message, string title = "错误");
}

public enum MessageType
{
    Info,
    Warning,
    Error,
    Success
}
