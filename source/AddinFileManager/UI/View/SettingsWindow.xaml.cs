using AddinFileManager.Common;
using System.Collections.ObjectModel;
using System.Linq;
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
