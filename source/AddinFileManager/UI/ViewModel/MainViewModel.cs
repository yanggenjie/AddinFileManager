using AddinFileManager.Common;
using AddinFileManager.Services;
using AddinFileManager.UI.Model;
using PropertyChanged;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace AddinFileManager.UI.ViewModel;

[AddINotifyPropertyChangedInterface]
public class MainViewModel
{
    private readonly IAddinFileService _addinFileService;
    private readonly IDialogService _dialogService;
    private readonly IOperationHistoryService _historyService;

    private ICommand _batchEnableCommand;
    private ICommand _batchDisableCommand;
    private ICommand _refreshCommand;
    private ICommand _undoCommand;
    private ICommand _toggleAddinCommand;
    private ICommand _deleteAddinCommand;
    private ICommand _openFolderCommand;
    private ICommand _showDetailsCommand;
    private ICommand _selectAllCommand;
    private ICommand _themeToggleCommand;

    /// <summary>
    /// 当前选中的版本
    /// </summary>
    [OnChangedMethod(nameof(OnSelectedVersionChanged))]
    public string SelectedVersion { get; set; }

    /// <summary>
    /// 搜索文本
    /// </summary>
    public string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// 是否深色主题
    /// </summary>
    public bool IsDarkTheme { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdateTime { get; set; }

    /// <summary>
    /// 操作提示消息
    /// </summary>
    public string StatusMessage { get; set; } = "就绪";

    /// <summary>
    /// 总数
    /// </summary>
    public int TotalCount => AddinFileItems.Count;

    /// <summary>
    /// 已启用数
    /// </summary>
    public int EnabledCount => AddinFileItems.Count(x => x.IsOn);

    /// <summary>
    /// 已禁用数
    /// </summary>
    public int DisabledCount => AddinFileItems.Count(x => !x.IsOn);

    /// <summary>
    /// 当前版本插件数（用于版本徽章）
    /// </summary>
    public int CurrentVersionCount => TotalCount;

    /// <summary>
    /// 是否可以撤销
    /// </summary>
    public bool CanUndo => _historyService.CanUndo;

    /// <summary>
    /// 全选状态
    /// </summary>
    public bool IsAllSelected
    {
        get => AddinCollectionView.Cast<AddinInfoModel>().Any() &&
               AddinCollectionView.Cast<AddinInfoModel>().All(x => x.IsSelected);
        set
        {
            foreach (var item in AddinCollectionView.Cast<AddinInfoModel>())
                item.IsSelected = value;
        }
    }

    /// <summary>
    /// 插件列表视图
    /// </summary>
    public ICollectionView AddinCollectionView { get; private set; }

    /// <summary>
    /// Revit 版本列表
    /// </summary>
    [DoNotNotify]
    public ObservableCollection<VersionInfo> RevitVersionItems { get; set; } = new();

    /// <summary>
    /// 插件列表（原始数据）
    /// </summary>
    [DoNotNotify]
    public ObservableCollection<AddinInfoModel> AddinFileItems { get; set; } = new();

    #region Commands

    public ICommand BatchEnableCommand => _batchEnableCommand ??= new RelayCommand(
        _ => BatchToggle(true),
        _ => AddinCollectionView.Cast<AddinInfoModel>().Any(item => item.IsSelected && !item.IsOn));

    public ICommand BatchDisableCommand => _batchDisableCommand ??= new RelayCommand(
        _ => BatchToggle(false),
        _ => AddinCollectionView.Cast<AddinInfoModel>().Any(item => item.IsSelected && item.IsOn));

    public ICommand RefreshCommand => _refreshCommand ??= new RelayCommand(_ => Refresh());

    public ICommand UndoCommand => _undoCommand ??= new RelayCommand(_ => Undo(), _ => CanUndo);

    public ICommand ToggleAddinCommand => _toggleAddinCommand ??= new RelayCommand(
        param => ToggleAddin(param as AddinInfoModel));

    public ICommand DeleteAddinCommand => _deleteAddinCommand ??= new RelayCommand(
        param => DeleteAddin(param as AddinInfoModel));

    public ICommand OpenFolderCommand => _openFolderCommand ??= new RelayCommand(
        param => OpenFolder(param as AddinInfoModel));

    public ICommand ShowDetailsCommand => _showDetailsCommand ??= new RelayCommand(
        param => ShowDetails(param as AddinInfoModel));

    public ICommand SelectAllCommand => _selectAllCommand ??= new RelayCommand(_ => ToggleSelectAll());

    public ICommand ThemeToggleCommand => _themeToggleCommand ??= new RelayCommand(_ => ToggleTheme());

    #endregion

    public MainViewModel()
        : this(new AddinFileService(), new DialogService(), new OperationHistoryService())
    {
    }

    public MainViewModel(IAddinFileService addinFileService, IDialogService dialogService, IOperationHistoryService historyService)
    {
        _addinFileService = addinFileService;
        _dialogService = dialogService;
        _historyService = historyService;

        InitializeCollectionView();
        LoadVersions();

        if (RevitVersionItems.Count > 0)
        {
            SelectedVersion = RevitVersionItems.FirstOrDefault(v => v.Version == "Autodesk Revit 2020")?.Version
                ?? RevitVersionItems.Last().Version;
        }

        Refresh();
    }

    private void InitializeCollectionView()
    {
        var viewSource = new CollectionViewSource { Source = AddinFileItems };
        viewSource.Filter += OnFilter;
        AddinCollectionView = viewSource.View;
    }

    private void OnFilter(object sender, FilterEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            e.Accepted = true;
            return;
        }

        if (e.Item is AddinInfoModel item)
        {
            var search = SearchText.ToLowerInvariant();
            e.Accepted = item.AddinFileName?.ToLowerInvariant().Contains(search) == true ||
                         item.Remark?.ToLowerInvariant().Contains(search) == true;
        }
    }

    private void OnSelectedVersionChanged()
    {
        Refresh();
    }

    public void Refresh()
    {
        AddinFileItems.Clear();
        _historyService.Clear();
        SearchText = string.Empty;
        StatusMessage = "正在加载...";

        var version = SelectedVersion?.Split(' ').LastOrDefault();
        if (string.IsNullOrWhiteSpace(version))
        {
            StatusMessage = "请选择 Revit 版本";
            return;
        }

        try
        {
            var addins = _addinFileService.GetAddinFiles(version);
            foreach (var addin in addins)
            {
                AddinFileItems.Add(addin);
            }

            LastUpdateTime = DateTime.Now;
            StatusMessage = $"已加载 {TotalCount} 个插件";

            UpdateVersionCounts();
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
            _dialogService.ShowError($"加载插件列表失败: {ex.Message}");
        }
    }

    private void LoadVersions()
    {
        var config = ConfigManager.LoadConfig();
        RevitVersionItems.Clear();

        foreach (var v in config.RevitVersions)
        {
            var versionNumber = v.Split(' ').LastOrDefault();
            var count = GetAddinCountForVersion(versionNumber);
            RevitVersionItems.Add(new VersionInfo { Version = v, Count = count });
        }
    }

    private int GetAddinCountForVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return 0;

        var count = 0;
        var addins = _addinFileService.GetAddinFiles(version);
        count = addins.Count();

        return count;
    }

    private void UpdateVersionCounts()
    {
        var currentVersion = SelectedVersion?.Split(' ').LastOrDefault();
        if (string.IsNullOrWhiteSpace(currentVersion)) return;

        var currentItem = RevitVersionItems.FirstOrDefault(v => v.Version == SelectedVersion);
        if (currentItem != null)
        {
            currentItem.Count = TotalCount;
        }
    }

    private void BatchToggle(bool enable)
    {
        var selectedItems = AddinCollectionView.Cast<AddinInfoModel>()
            .Where(x => x.IsSelected)
            .ToList();

        if (!selectedItems.Any()) return;

        var successCount = 0;
        foreach (var item in selectedItems)
        {
            if (item.IsOn != enable)
            {
                try
                {
                    RecordOperation(item);
                    _addinFileService.ToggleAddin(item, enable);
                    item.IsOn = enable;
                    successCount++;
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError(ex.Message);
                }
            }
        }

        StatusMessage = enable
            ? $"已批量启用 {successCount} 个插件"
            : $"已批量禁用 {successCount} 个插件";
    }

    private void ToggleAddin(AddinInfoModel item)
    {
        if (item == null) return;

        try
        {
            RecordOperation(item);
            _addinFileService.ToggleAddin(item, item.IsOn);
            StatusMessage = item.IsOn ? $"已启用 {item.Remark}" : $"已禁用 {item.Remark}";
        }
        catch (Exception ex)
        {
            item.IsOn = !item.IsOn; // 回滚状态
            _dialogService.ShowError(ex.Message);
            StatusMessage = "操作失败";
        }
    }

    private void DeleteAddin(AddinInfoModel item)
    {
        if (item == null) return;

        if (!_dialogService.ShowConfirm($"确定要删除插件文件 {item.AddinFileName} 吗？\n删除后将无法恢复！", "删除确认"))
            return;

        try
        {
            RecordOperation(item);
            _addinFileService.DeleteAddin(item);
            AddinFileItems.Remove(item);
            StatusMessage = $"已删除 {item.Remark}";
            UpdateVersionCounts();
        }
        catch (Exception ex)
        {
            _historyService.RemoveLastOperation();
            _dialogService.ShowError(ex.Message);
            StatusMessage = "删除失败";
        }
    }

    private void OpenFolder(AddinInfoModel item)
    {
        if (item == null) return;

        try
        {
            _addinFileService.OpenFolder(item.FileFullPath);
            StatusMessage = $"已打开 {item.InstallLocation}";
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    private void ShowDetails(AddinInfoModel item)
    {
        if (item == null) return;

        item.LoadFullInfo();
        // 详情窗口将在 View 层处理
        StatusMessage = $"查看 {item.Remark} 详情";
    }

    private void ToggleSelectAll()
    {
        var items = AddinCollectionView.Cast<AddinInfoModel>().ToList();
        var allSelected = items.All(x => x.IsSelected);

        foreach (var item in items)
            item.IsSelected = !allSelected;

        StatusMessage = allSelected ? "已取消全选" : "已全选";
    }

    private void Undo()
    {
        var lastOperation = _historyService.GetLastOperation();
        if (lastOperation == null) return;

        try
        {
            if (lastOperation.Type == OperationType.ToggleEnable)
            {
                _addinFileService.ToggleAddin(lastOperation.Model, lastOperation.PreviousState);
                lastOperation.Model.IsOn = lastOperation.PreviousState;
                lastOperation.Model.FileFullPath = lastOperation.PreviousFilePath;
                lastOperation.Model.AddinFileName = lastOperation.PreviousFileName;
                StatusMessage = $"已撤销: {lastOperation.Model.Remark}";
            }
            else if (lastOperation.Type == OperationType.Delete)
            {
                // 删除操作无法撤销（文件已删除）
                StatusMessage = "删除操作无法撤销";
            }

            _historyService.RemoveLastOperation();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"撤销失败: {ex.Message}");
        }
    }

    private void RecordOperation(AddinInfoModel item)
    {
        _historyService.AddOperation(new OperationHistory
        {
            Type = OperationType.ToggleEnable,
            Model = item,
            PreviousState = item.IsOn,
            PreviousFilePath = item.FileFullPath,
            PreviousFileName = item.AddinFileName,
        });
    }

    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        App.ToggleTheme();
        StatusMessage = IsDarkTheme ? "已切换到深色主题" : "已切换到浅色主题";
    }

    /// <summary>
    /// 应用搜索过滤
    /// </summary>
    public void ApplyFilter()
    {
        AddinCollectionView.Refresh();
    }

    /// <summary>
    /// 刷新版本列表（设置窗口保存后调用）
    /// </summary>
    public void ReloadVersions(string previousVersion)
    {
        LoadVersions();
        if (RevitVersionItems.Any(v => v.Version == previousVersion))
        {
            SelectedVersion = previousVersion;
        }
        else if (RevitVersionItems.Count > 0)
        {
            SelectedVersion = RevitVersionItems.Last().Version;
        }
    }
}

/// <summary>
/// 版本信息（用于显示徽章）
/// </summary>
[AddINotifyPropertyChangedInterface]
public class VersionInfo
{
    public string Version { get; set; }
    public int Count { get; set; }
}