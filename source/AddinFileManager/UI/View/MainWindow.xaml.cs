using AddinFileManager.UI.View;
using AddinFileManager.UI.ViewModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AddinFileManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new MainViewModel();
        
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        this.Title = $"Revit插件管理器 v{version.Major}.{version.Minor}.{version.Build} - Copyright © RyzeYang 2024";
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Owner = this;
        if (settingsWindow.ShowDialog() == true)
        {
            if (this.DataContext is MainViewModel vm)
            {
                var previousSelected = vm.SelectedVersion;
                vm.LoadVersions();
                if (vm.RevitVersionItems.Contains(previousSelected))
                {
                    vm.SelectedVersion = previousSelected;
                }
                else if (vm.RevitVersionItems.Count > 0)
                {
                    vm.SelectedVersion = vm.RevitVersionItems[vm.RevitVersionItems.Count - 1];
                }
            }
        }
    }
}