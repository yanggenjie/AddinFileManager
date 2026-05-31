using System;
using System.Linq;
using System.Windows;
using AddinFileManager.UI.View;

namespace AddinFileManager.Services;

/// <summary>
/// 对话框服务实现
/// </summary>
public class DialogService : IDialogService
{
    public bool ShowConfirm(string message, string title = "确认")
    {
        bool result = false;
        Application.Current.Dispatcher.Invoke(() =>
        {
            var activeWindow = Application.Current.Windows.OfType<Window>()
                .SingleOrDefault(x => x.IsActive) ?? Application.Current.MainWindow;
            var dialog = new ConfirmWindow(message, title) { Owner = activeWindow };
            result = dialog.ShowDialog() == true;
        });
        return result;
    }

    public void ShowMessage(string message, string title = "提示", MessageType type = MessageType.Info)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var activeWindow = Application.Current.Windows.OfType<Window>()
                .SingleOrDefault(x => x.IsActive) ?? Application.Current.MainWindow;
            var dialog = new MessageWindow(message, title, type) { Owner = activeWindow };
            dialog.ShowDialog();
        });
    }

    public void ShowError(string message, string title = "错误")
    {
        ShowMessage(message, title, MessageType.Error);
    }
}
