using AddinFileManager.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AddinFileManager.UI.View
{
    public partial class SettingsWindow : Window
    {
        private static readonly Regex DigitsOnlyRegex = new(@"^\d+$", RegexOptions.Compiled);
        private static readonly Regex FourDigitYearRegex = new(@"^\d{4}$", RegexOptions.Compiled);
        private const string RepoOwner = "yanggenjie";
        private const string RepoName = "AddinFileManager";

        public ObservableCollection<string> Versions { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();
            var config = ConfigManager.LoadConfig();
            Versions = new ObservableCollection<string>(config.RevitVersions);
            VersionsListBox.ItemsSource = Versions;
            Versions.CollectionChanged += Versions_CollectionChanged;
            UpdateEmptyHint();

            var assembly = Assembly.GetExecutingAssembly();
            VersionTextBlock.Text = assembly.GetName().Version.ToString();

            var copyrightAttr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            if (copyrightAttr != null)
            {
                CopyrightTextBlock.Text = copyrightAttr.Copyright;
            }

            string location = assembly.Location;
            if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
            {
                var fileInfo = new System.IO.FileInfo(location);
                UpdateTimeTextBlock.Text = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
            }

            NewVersionTextBox.MaxLength = 4;
        }

        private void Versions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateEmptyHint();
        }

        private void UpdateEmptyHint()
        {
            if (EmptyListHint != null)
            {
                EmptyListHint.Visibility = Versions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void NewVersionTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !DigitsOnlyRegex.IsMatch(e.Text);
        }

        private void NewVersionTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!DigitsOnlyRegex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
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
            OpenUpdateLogUrl();
            e.Handled = true;
        }

        private void UpdateLogBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenUpdateLogUrl();
        }

        private static void OpenUpdateLogUrl()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://github.com/{RepoOwner}/{RepoName}/releases",
                UseShellExecute = true
            });
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdateButton.IsEnabled = false;
            UpdateStatusTextBlock.Text = "正在检查...";
            UpdateStatusTextBlock.Foreground = FindResource("MahApps.Brushes.Gray3") as System.Windows.Media.Brush;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "AddinFileManager");

                var response = await httpClient.GetStringAsync($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");
                var json = JObject.Parse(response);

                var latestTag = json["tag_name"]?.ToString();
                var latestVersion = latestTag?.TrimStart('v');
                var releaseUrl = json["html_url"]?.ToString();

                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                if (CompareVersions(latestVersion, currentVersion) > 0)
                {
                    UpdateStatusTextBlock.Text = $"发现新版本: v{latestVersion}";
                    UpdateStatusTextBlock.Foreground = FindResource("MahApps.Brushes.Accent") as System.Windows.Media.Brush;

                    var result = MessageBox.Show(
                        $"当前版本: v{currentVersion}\n最新版本: v{latestVersion}\n\n是否前往下载？",
                        "发现新版本",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes && !string.IsNullOrEmpty(releaseUrl))
                    {
                        Process.Start(new ProcessStartInfo { FileName = releaseUrl, UseShellExecute = true });
                    }
                }
                else
                {
                    UpdateStatusTextBlock.Text = "已是最新版本";
                    UpdateStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                UpdateStatusTextBlock.Text = "检查更新失败";
                UpdateStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"检查更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
            }
        }

        private static int CompareVersions(string v1, string v2)
        {
            var parts1 = v1?.Split('.') ?? Array.Empty<string>();
            var parts2 = v2?.Split('.') ?? Array.Empty<string>();

            for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
            {
                var p1 = i < parts1.Length && int.TryParse(parts1[i], out var n1) ? n1 : 0;
                var p2 = i < parts2.Length && int.TryParse(parts2[i], out var n2) ? n2 : 0;

                if (p1 > p2) return 1;
                if (p1 < p2) return -1;
            }
            return 0;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var text = NewVersionTextBox.Text.Trim();

            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("请输入版本号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!FourDigitYearRegex.IsMatch(text))
            {
                MessageBox.Show("版本号必须是4位数字，例如：2024、2025", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newVersion = $"Autodesk Revit {text}";

            if (Versions.Contains(newVersion))
            {
                MessageBox.Show($"版本 {text} 已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Versions.Add(newVersion);
            var sorted = Versions.OrderBy(v => v).ToList();
            Versions.Clear();
            foreach (var v in sorted)
            {
                Versions.Add(v);
            }
            NewVersionTextBox.Text = "";
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string version)
            {
                var confirmDialog = new ConfirmWindow($"确定要删除版本 \"{version}\" 吗？", "删除确认");
                confirmDialog.Owner = this;
                if (confirmDialog.ShowDialog() == true)
                {
                    Versions.Remove(version);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var config = new AppConfig
            {
                RevitVersions = Versions.ToList()
            };
            ConfigManager.SaveConfig(config);
            MessageBox.Show("保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
