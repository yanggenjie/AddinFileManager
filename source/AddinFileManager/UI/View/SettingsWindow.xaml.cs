using AddinFileManager.Common;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            Versions.CollectionChanged += Versions_CollectionChanged;
            UpdateEmptyHint();

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

            // 设置输入框最大长度
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

        /// <summary>
        /// 输入校验：仅允许数字输入
        /// </summary>
        private void NewVersionTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        /// <summary>
        /// 粘贴校验：仅允许粘贴数字
        /// </summary>
        private void NewVersionTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, @"^[0-9]+$"))
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
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://yanggenjie.cn/MySoft/RevitAddinfileManager.html",
                UseShellExecute = true
            });
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var text = NewVersionTextBox.Text.Trim();

            // 校验：非空检查
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("请输入版本号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 校验：必须是4位数字
            if (!Regex.IsMatch(text, @"^\d{4}$"))
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
            // Sort versions
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