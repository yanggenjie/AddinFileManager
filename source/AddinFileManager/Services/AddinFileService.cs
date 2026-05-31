using AddinFileManager.Common;
using AddinFileManager.UI.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AddinFileManager.Services;

/// <summary>
/// 插件文件服务实现
/// </summary>
public class AddinFileService : IAddinFileService
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly List<string> _defaultAddinFileNames =
    [
        "ExportViewSelectorApp",
        "Communicator",
        "FormItConverter",
        "BIM360GlueRevitAddin",
        "BIM360GlueRevit2016Addin",
        "Dynamo",
    ];

    public IEnumerable<AddinInfoModel> GetAddinFiles(string version)
    {
        var result = new List<AddinInfoModel>();
        if (string.IsNullOrWhiteSpace(version)) return result;

        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var appFolder = Path.Combine(commonAppData, @"Autodesk\Revit\Addins");
        result.AddRange(GetAddinInfosFromFolder(appFolder, version, "全局安装目录"));

        var userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var userFolder = Path.Combine(userProfileFolder, @"Autodesk\Revit\Addins");
        result.AddRange(GetAddinInfosFromFolder(userFolder, version, "用户安装目录"));

        return result;
    }

    public bool ToggleAddin(AddinInfoModel model, bool enable)
    {
        if (!File.Exists(model.FileFullPath)) return false;

        var fileExt = Path.GetExtension(model.FileFullPath);
        var fileName = Path.GetFileName(model.FileFullPath);
        var folder = Path.GetDirectoryName(model.FileFullPath);

        try
        {
            if (enable && fileExt == CommonString.DisableExt)
            {
                var newFileName = fileName.Replace(CommonString.DisableExt, "");
                var newFile = Path.Combine(folder, newFileName);
                if (File.Exists(newFile))
                {
                    File.Delete(model.FileFullPath);
                }
                else
                {
                    File.Move(model.FileFullPath, newFile);
                }
                model.FileFullPath = newFile;
                model.AddinFileName = newFileName;
            }
            else if (!enable && fileExt != CommonString.DisableExt)
            {
                var newFileName = fileName + CommonString.DisableExt;
                var newFile = Path.Combine(folder, newFileName);
                if (File.Exists(newFile))
                {
                    File.Delete(newFile);
                }
                File.Move(model.FileFullPath, newFile);
                model.FileFullPath = newFile;
                model.AddinFileName = newFileName;
            }
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException("权限不足，请以管理员身份运行此程序。");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"修改文件失败: {ex.Message}");
        }
    }

    public bool DeleteAddin(AddinInfoModel model)
    {
        try
        {
            if (File.Exists(model.FileFullPath))
            {
                File.Delete(model.FileFullPath);
            }
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException("权限不足，请以管理员身份运行此程序以删除文件。");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"删除文件失败: {ex.Message}");
        }
    }

    public void OpenFolder(string fileFullPath)
    {
        var folder = Path.GetDirectoryName(fileFullPath);
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
            throw new InvalidOperationException($"无法打开目录: {ex.Message}");
        }
    }

    private IEnumerable<AddinInfoModel> GetAddinInfosFromFolder(string addinFolder, string version, string installLocation)
    {
        var result = new List<AddinInfoModel>();
        var currentVersion = Path.Combine(addinFolder, version);
        if (!Directory.Exists(currentVersion)) return result;

        try
        {
            var allFiles = Directory.GetFiles(currentVersion, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f);
                    return ext.Equals(".addin", StringComparison.OrdinalIgnoreCase) ||
                           ext.Equals(CommonString.DisableExt, StringComparison.OrdinalIgnoreCase);
                }).ToList();

            var groupedFiles = allFiles.GroupBy(
                f => f.EndsWith(CommonString.DisableExt, StringComparison.OrdinalIgnoreCase)
                    ? f.Substring(0, f.Length - CommonString.DisableExt.Length)
                    : f,
                StringComparer.OrdinalIgnoreCase);

            var validFiles = new List<string>();
            foreach (var group in groupedFiles)
            {
                var files = group.ToList();
                if (files.Count > 1)
                {
                    var disableFile = files.FirstOrDefault(f =>
                        f.EndsWith(CommonString.DisableExt, StringComparison.OrdinalIgnoreCase));
                    if (disableFile != null)
                    {
                        try { File.Delete(disableFile); } catch { }
                    }
                    validFiles.Add(group.Key);
                }
                else
                {
                    validFiles.Add(files.First());
                }
            }

            var addinFiles = validFiles.Where(f =>
            {
                var name = Path.GetFileName(f);
                var baseName = name.EndsWith(CommonString.DisableExt, StringComparison.OrdinalIgnoreCase)
                    ? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(f))
                    : Path.GetFileNameWithoutExtension(f);

                return !baseName.StartsWith("Autodesk", StringComparison.OrdinalIgnoreCase) &&
                       !_defaultAddinFileNames.Contains(baseName);
            });

            foreach (var file in addinFiles)
            {
                var fileName = Path.GetFileName(file);
                var fileExt = Path.GetExtension(file);

                var addinInfo = new AddinInfoModel
                {
                    FileFullPath = file,
                    InstallLocation = installLocation,
                    AddinFileName = fileName,
                    IsOn = !fileExt.Equals(CommonString.DisableExt, StringComparison.OrdinalIgnoreCase),
                };

                try
                {
                    var nameLine = File.ReadLines(file).FirstOrDefault(x => x.Contains("<Name>"));
                    if (nameLine != null)
                    {
                        var addinName = nameLine.Replace("<Name>", "").Replace("</Name>", "").Replace(" ", "");
                        addinInfo.Remark = WhitespaceRegex.Replace(addinName, "");
                    }
                }
                catch { }

                result.Add(addinInfo);
            }
        }
        catch { }

        return result;
    }
}
