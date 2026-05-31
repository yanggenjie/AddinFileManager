using AddinFileManager.UI.Model;
using AddinFileManager.UI.View;
using AddinFileManager.UI.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AddinFileManager;

public partial class MainWindow : Window
{
    private MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        Title = $"Revit插件管理器 v{version.Major}.{version.Minor}.{version.Build} - Copyright © RyzeYang 2026";
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Owner = this;
        if (settingsWindow.ShowDialog() == true)
        {
            var previousSelected = _viewModel.SelectedVersion;
            _viewModel.ReloadVersions(previousSelected);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.ApplyFilter();
    }

    private void AddinDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // 直接使用当前选中项
        if (AddinDataGrid.SelectedItem is AddinInfoModel model)
        {
            ShowAddinDetails(model);
            e.Handled = true;
        }
    }

    private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T typed)
                return typed;
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        // Delete 键删除选中项
        if (e.Key == Key.Delete && AddinDataGrid.SelectedItem is AddinInfoModel model)
        {
            _viewModel.DeleteAddinCommand.Execute(model);
            e.Handled = true;
        }
    }

    private void ShowAddinDetails(AddinInfoModel model)
    {
        var detailsWindow = new AddinDetailsWindow(model);
        detailsWindow.Owner = this;
        detailsWindow.ShowDialog();
    }
}