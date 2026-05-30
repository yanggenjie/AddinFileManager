using AddinFileManager.Common;
using AddinFileManager.UI.Model;
using Commander;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace AddinFileManager.UI.ViewModel;

[AddINotifyPropertyChangedInterface]
public class MainViewModel
{
    private readonly List<string> DefaultAddinFileNames =
    [
        "ExportViewSelectorApp",
        "Communicator",
        "FormItConverter",
        "BIM360GlueRevitAddin",
        "BIM360GlueRevit2016Addin",
        "Dynamo",
    ];

    private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

    [OnChangedMethod(nameof(OnSelectedVersionChanged))]
    public string SelectedVersion { get; set; }

    /// <summary>
    /// 搜索文本
    /// </summary>
    [AlsoNotifyFor(nameof(FilteredAddinItems))]
    public string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// 总插件数量
    /// </summary>
    public int TotalCount => AddinFileItems.Count;

    /// <summary>
    /// 已启用插件数量
    /// </summary>
    public int EnabledCount => AddinFileItems.Count(x => x.IsOn);

    /// <summary>
    /// 已禁用插件数量
    /// </summary>
    public int DisabledCount => AddinFileItems.Count(x => !x.IsOn);

    /// <summary>
    /// 过滤后的插件数量
    /// </summary>
    public int FilteredCount => FilteredAddinItems.Count();

    /// <summary>
    /// 过滤后的插件列表
    /// </summary>
    public IEnumerable<AddinInfoModel> FilteredAddinItems
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return AddinFileItems;

            var search = SearchText.ToLowerInvariant();
            return AddinFileItems.Where(x =>
                x.AddinFileName?.ToLowerInvariant().Contains(search) == true ||
                x.Remark?.ToLowerInvariant().Contains(search) == true);
        }
    }

    /// <summary>
    /// 全选复选框绑定
    /// </summary>
    public bool IsAllSelected
    {
        get => FilteredAddinItems.Any() && FilteredAddinItems.All(x => x.IsSelected);
        set
        {
            foreach (var item in FilteredAddinItems)
            {
                item.IsSelected = value;
            }
        }
    }

    /// <summary>
    /// 批量启用命令
    /// </summary>
    public ICommand BatchEnableCommand => new RelayCommand(
        _ => BatchEnable(true),
        _ => FilteredAddinItems.Any(item => item.IsSelected && !item.IsOn));

    /// <summary>
    /// 批量禁用命令
    /// </summary>
    public ICommand BatchDisableCommand => new RelayCommand(
        _ => BatchEnable(false),
        _ => FilteredAddinItems.Any(item => item.IsSelected && item.IsOn));

    /// <summary>
    /// 刷新命令
    /// </summary>
    public ICommand RefreshCommand => new RelayCommand(_ => OnSelectedVersionChanged());

    private void OnSelectedVersionChanged()
    {
        AddinFileItems.Clear();
        SearchText = string.Empty;
        var version = SelectedVersion?.Split(' ').LastOrDefault();
        if (string.IsNullOrWhiteSpace(version)) return;

        // 全局目录
        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var appFolder = Path.Combine(commonAppData, @"Autodesk\Revit\Addins");
        GetApplicationAddinInfos(appFolder, version, "全局安装目录");

        // 用户目录
        var userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var userFolder = Path.Combine(userProfileFolder, @"Autodesk\Revit\Addins");
        GetApplicationAddinInfos(userFolder, version, "用户安装目录");
    }

    private void GetApplicationAddinInfos(string addinFolder, string version, string installLocation)
    {
        if (string.IsNullOrWhiteSpace(version)) return;

        var currentVersion = Path.Combine(addinFolder, version);
        if (!Directory.Exists(currentVersion)) return;

        try
        {
            var allFiles = Directory.GetFiles(currentVersion, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f);
                    return ext.Equals(".addin", StringComparison.OrdinalIgnoreCase) || ext.Equals(CommonString.DisableExt, StringComparison.OrdinalIgnoreCase);
                }).ToList();

            var groupedFiles = allFiles.GroupBy(f => f.EndsWith(CommonString.DisableExt, StringComparison.OrdinalIgnoreCase) ? f.Substring(0, f.Length - CommonString.DisableExt.Length) : f, StringComparer.OrdinalIgnoreCase);

            var validFiles = new List<string>();
            foreach (var group in groupedFiles)
            {
                var files = group.ToList();
                if (files.Count > 1)
                {
                    var disableFile = files.FirstOrDefault(f => f.EndsWith(CommonString.DisableExt, StringComparison.OrdinalIgnoreCase));
                    if (disableFile != null)
                    {
                        try
                        {
                            File.Delete(disableFile);
                        }
                        catch
                        {
                        }
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

                return !baseName.StartsWith("Autodesk", StringComparison.OrdinalIgnoreCase) && !DefaultAddinFileNames.Contains(baseName);
            });

            foreach (var file in addinFiles)
            {
                var fileName = Path.GetFileName(file);
                var fileExt = Path.GetExtension(file);

                var addinInfo = new AddinInfoModel()
                {
                    FileFullPath = file,
                    InstallLocation = installLocation,
                    AddinFileName = fileName,
                    IsOn = !fileExt.Equals(CommonString.DisableExt, StringComparison.OrdinalIgnoreCase),
                };

                addinInfo.DeleteAction = (model) => AddinFileItems.Remove(model);

                try
                {
                    var nameLine = File.ReadLines(file).FirstOrDefault(x => x.Contains("<Name>"));
                    if (nameLine != null)
                    {
                        var addinName = nameLine.Replace("<Name>", "").Replace("</Name>", "").Replace(" ", "");
                        addinInfo.Remark = WhitespaceRegex.Replace(addinName, "");
                    }
                }
                catch (Exception)
                {
                    // Ignore file read errors for individual files
                }

                AddinFileItems.Add(addinInfo);
            }
        }
        catch (Exception)
        {
            // Ignore directory access errors
        }
    }

    /// <summary>
    /// 批量启用/禁用
    /// </summary>
    private void BatchEnable(bool enable)
    {
        var selectedItems = FilteredAddinItems.Where(x => x.IsSelected).ToList();
        foreach (var item in selectedItems)
        {
            item.IsOn = enable;
        }
    }

    [DoNotNotify]
    public ObservableCollection<string> RevitVersionItems { get; set; } = new();

    [DoNotNotify]
    public ObservableCollection<AddinInfoModel> AddinFileItems { get; set; } = new();

    public MainViewModel()
    {
        LoadVersions();
        if (RevitVersionItems.Count > 0)
        {
            SelectedVersion = RevitVersionItems.Contains("Autodesk Revit 2020") ? "Autodesk Revit 2020" : RevitVersionItems[RevitVersionItems.Count - 1];
        }

        // PropertyChanged.Fody does not weave OnChanged methods for assignments in the constructor.
        // Therefore, we need to explicitly call the change handler to load the initial list.
        OnSelectedVersionChanged();
    }

    public void LoadVersions()
    {
        var config = ConfigManager.LoadConfig();
        RevitVersionItems.Clear();
        foreach (var v in config.RevitVersions)
        {
            RevitVersionItems.Add(v);
        }
    }
}
