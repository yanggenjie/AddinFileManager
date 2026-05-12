using AddinFileManager.Common;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace AddinFileManager.UI.View
{
    public partial class SettingsWindow : Window
    {
        public ObservableCollection<string> Versions { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();
            var config = ConfigManager.LoadConfig();
            Versions = new ObservableCollection<string>(config.RevitVersions);
            VersionsListBox.ItemsSource = Versions;

            // 设置关于信息
            var assembly = Assembly.GetExecutingAssembly();
            VersionTextBlock.Text = assembly.GetName().Version.ToString();
            
            // 获取版权信息
            var copyrightAttr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            if (copyrightAttr != null)
            {
                CopyrightTextBlock.Text = copyrightAttr.Copyright;
            }

            // 获取更新时间（通过程序集的最后写入时间）
            string location = assembly.Location;
            if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
            {
                var fileInfo = new System.IO.FileInfo(location);
                UpdateTimeTextBlock.Text = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
            }
        }

        private void NavListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavListBox.SelectedIndex == 0)
            {
                if (VersionSettingsPanel != null) VersionSettingsPanel.Visibility = Visibility.Visible;
                if (AboutPanel != null) AboutPanel.Visibility = Visibility.Collapsed;
            }
            else if (NavListBox.SelectedIndex == 1)
            {
                if (VersionSettingsPanel != null) VersionSettingsPanel.Visibility = Visibility.Collapsed;
                if (AboutPanel != null) AboutPanel.Visibility = Visibility.Visible;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var text = NewVersionTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            string newVersion = text;
            if (int.TryParse(text, out _))
            {
                newVersion = $"Autodesk Revit {text}";
            }

            if (!Versions.Contains(newVersion))
            {
                Versions.Add(newVersion);
                // Sort versions
                var sorted = Versions.OrderBy(v => v).ToList();
                Versions.Clear();
                foreach (var v in sorted)
                {
                    Versions.Add(v);
                }
            }
            NewVersionTextBox.Text = "";
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string version)
            {
                Versions.Remove(version);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var config = new AppConfig
            {
                RevitVersions = Versions.ToList()
            };
            ConfigManager.SaveConfig(config);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}