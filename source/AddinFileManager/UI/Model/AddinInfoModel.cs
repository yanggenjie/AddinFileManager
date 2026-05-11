using AddinFileManager.Common;
using Commander;
using PropertyChanged;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System;

namespace AddinFileManager.UI.Model;

[AddINotifyPropertyChangedInterface]
public class AddinInfoModel
{
    public string InstallLocation { get; set; }

    public string AddinFileName { get; set; }

    public string Remark { get; set; }

    [OnChangedMethod(nameof(OnIsOnChanged))]
    public bool IsOn { get; set; }
    public ICommand OpenFolderCommand => new RelayCommand(x => OpenFolder());
    
    public Action<AddinInfoModel> DeleteAction { get; set; }
    
    public ICommand DeleteCommand => new RelayCommand(x => DeleteAddin());

    private void DeleteAddin()
    {
        var result = MessageBox.Show($"确定要删除插件文件 {AddinFileName} 吗？\n删除后将无法恢复！", "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                if (File.Exists(FileFullPath))
                {
                    File.Delete(FileFullPath);
                }
                DeleteAction?.Invoke(this);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("权限不足，请以管理员身份运行此程序以删除文件。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool _isUpdating;
    private void OnIsOnChanged()
    {
        if (_isUpdating) return;
        if (!File.Exists(FileFullPath)) return;
        var fileExt = Path.GetExtension(FileFullPath);
        var fileName = Path.GetFileName(FileFullPath);
        var folder = Path.GetDirectoryName(FileFullPath);

        try
        {
            if (IsOn && fileExt == CommonString.DisableExt)
            {
                fileName = fileName.Replace(CommonString.DisableExt, "");
                var newFile = Path.Combine(folder, fileName);
                if (File.Exists(newFile))
                {
                    // 存在.addin文件时，直接删除当前的.disable文件
                    File.Delete(FileFullPath);
                }
                else
                {
                    File.Move(FileFullPath, newFile);
                }
                FileFullPath = newFile;
                AddinFileName = fileName;
            }
            else if (!IsOn && fileExt != CommonString.DisableExt)
            {
                fileName = fileName + CommonString.DisableExt;
                var newFile = Path.Combine(folder, fileName);
                if (File.Exists(newFile))
                {
                    // 存在旧的.disable文件时，直接删除旧的.disable文件
                    File.Delete(newFile);
                }
                File.Move(FileFullPath, newFile);
                FileFullPath = newFile;
                AddinFileName = fileName;
            }
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("权限不足，请以管理员身份运行此程序。");
            RevertIsOn();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"修改文件失败: {ex.Message}");
            RevertIsOn();
        }
    }

    private void RevertIsOn()
    {
        _isUpdating = true;
        IsOn = !IsOn;
        _isUpdating = false;
    }

    public string FileFullPath { get; set; }

    private void OpenFolder()
    {
        var folder = Path.GetDirectoryName(FileFullPath);
        if (!Directory.Exists(folder)) return;
        
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true,
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开目录: {ex.Message}");
        }
    }
}