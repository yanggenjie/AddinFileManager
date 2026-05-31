using AddinFileManager.UI.Model;
using AddinFileManager.UI.ViewModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace AddinFileManager.UI.View;

public partial class AddinDetailsWindow : Window
{
    private readonly AddinInfoModel _model;
    private readonly MainViewModel _viewModel;
    private readonly bool _originalIsOn;
    private readonly string _originalXmlContent;
    private Dictionary<string, string> _fieldValues = new();

    public AddinDetailsWindow(AddinInfoModel model, MainViewModel viewModel = null)
    {
        InitializeComponent();
        _model = model;
        _viewModel = viewModel;
        _originalIsOn = model.IsOn;
        _originalXmlContent = File.Exists(_model.FileFullPath)
            ? File.ReadAllText(_model.FileFullPath)
            : string.Empty;
        LoadData();
    }

    private void LoadData()
    {
        TitleName.Text = _model.Remark ?? _model.AddinFileName;
        FileNameText.Text = _model.AddinFileName;
        LocationText.Text = _model.InstallLocation;
        FullPathText.Text = _model.FileFullPath;

        // 状态
        UpdateStatusDisplay(_model.IsOn);
        StatusToggle.IsOn = _model.IsOn;
        StatusToggle.Toggled += (s, e) => UpdateStatusDisplay(StatusToggle.IsOn);

        // 动态解析XML并生成字段
        ParseAndGenerateFields();
    }

    private void ParseAndGenerateFields()
    {
        if (string.IsNullOrEmpty(_originalXmlContent)) return;

        try
        {
            var doc = XDocument.Parse(_originalXmlContent);
            var addinElement = doc.Root?.Element("AddIn") ?? doc.Root;

            if (addinElement == null) return;

            foreach (var element in addinElement.Elements())
            {
                var name = element.Name.LocalName;
                var value = element.Value ?? string.Empty;
                _fieldValues[name] = value;

                // 创建标签和输入框
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // 标签
                var label = new TextBlock
                {
                    Text = name + ":",
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666666")),
                    FontWeight = FontWeights.Medium,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                // 输入框（某些字段只读）
                var isReadOnly = name == "AddInType";
                if (isReadOnly)
                {
                    var textBlock = new TextBlock
                    {
                        Text = value,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333")),
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    };
                    Grid.SetColumn(textBlock, 1);
                    grid.Children.Add(textBlock);
                }
                else
                {
                    var textBox = new TextBox
                    {
                        Text = value,
                        VerticalAlignment = VerticalAlignment.Center,
                        Padding = new Thickness(6, 4, 6, 4),
                        BorderBrush = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0")),
                        Background = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FAFAFA")),
                        Tag = name // 保存字段名用于后续保存
                    };
                    textBox.ToolTip = new ToolTip { Content = value };
                    Grid.SetColumn(textBox, 1);
                    grid.Children.Add(textBox);
                }

                FieldsPanel.Children.Add(grid);
            }
        }
        catch
        {
            // XML解析失败，忽略
        }
    }

    private void UpdateStatusDisplay(bool isOn)
    {
        StatusText.Text = isOn ? "已启用" : "已禁用";
        StatusText.Foreground = isOn
            ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#43A047")
            : (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#E53935");
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = Path.GetDirectoryName(_model.FileFullPath);
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
        catch { }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // 保存启用状态
        if (StatusToggle.IsOn != _originalIsOn)
        {
            _viewModel?.ToggleAddinCommand.Execute(_model);
        }

        // 保存插件信息到文件
        if (SaveAddinInfo())
        {
            MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
    }

    private bool SaveAddinInfo()
    {
        var filePath = _model.FileFullPath;
        if (!File.Exists(filePath)) return false;

        try
        {
            var xmlContent = _originalXmlContent;

            // 遍历所有输入框，更新XML内容
            foreach (var child in FieldsPanel.Children)
            {
                if (child is Grid grid && grid.Children[1] is TextBox textBox)
                {
                    var fieldName = textBox.Tag?.ToString();
                    if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(textBox.Text))
                    {
                        xmlContent = UpdateXmlTag(xmlContent, fieldName, textBox.Text);

                        // 如果是Name字段，同步更新模型
                        if (fieldName == "Name")
                        {
                            _model.Remark = textBox.Text;
                        }
                    }
                }
            }

            File.WriteAllText(filePath, xmlContent);
            return true;
        }
        catch
        {
            MessageBox.Show("保存文件失败，请检查文件权限", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private static string UpdateXmlTag(string xml, string tagName, string value)
    {
        var startTag = $"<{tagName}>";
        var endTag = $"</{tagName}>";

        var startIndex = xml.IndexOf(startTag);
        var endIndex = xml.IndexOf(endTag);

        if (startIndex >= 0 && endIndex > startIndex)
        {
            var before = xml.Substring(0, startIndex + startTag.Length);
            var after = xml.Substring(endIndex);
            return before + System.Security.SecurityElement.Escape(value) + after;
        }

        return xml;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
