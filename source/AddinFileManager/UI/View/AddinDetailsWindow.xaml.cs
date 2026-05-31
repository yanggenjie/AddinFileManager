using AddinFileManager.Services;
using AddinFileManager.UI.Model;
using AddinFileManager.UI.ViewModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace AddinFileManager.UI.View;

public partial class AddinDetailsWindow : Window
{
    private readonly AddinInfoModel _model;
    private readonly MainViewModel _viewModel;
    private AddinFullInfo _originalInfo;
    private readonly bool _originalIsOn;

    public AddinDetailsWindow(AddinInfoModel model, MainViewModel viewModel = null)
    {
        InitializeComponent();
        _model = model;
        _viewModel = viewModel;
        _originalIsOn = model.IsOn;
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

        var fullInfo = _model.LoadFullInfo();
        if (fullInfo != null)
        {
            _originalInfo = fullInfo;
            NameTextBox.Text = fullInfo.Name ?? string.Empty;
            TypeText.Text = fullInfo.AddinType ?? string.Empty;
            AssemblyTextBox.Text = fullInfo.Assembly ?? string.Empty;
            ClassNameTextBox.Text = fullInfo.FullClassName ?? string.Empty;
            VendorIdTextBox.Text = fullInfo.VendorId ?? string.Empty;
            VendorDescTextBox.Text = fullInfo.VendorDescription ?? string.Empty;
        }
    }

    private void UpdateStatusDisplay(bool isOn)
    {
        StatusText.Text = isOn ? "已启用" : "已禁用";
        StatusText.Foreground = isOn
            ? (Brush)new BrushConverter().ConvertFrom("#43A047")!
            : (Brush)new BrushConverter().ConvertFrom("#E53935")!;
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
        SaveAddinInfo();

        MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private void SaveAddinInfo()
    {
        if (_originalInfo == null) return;

        var filePath = _model.FileFullPath;
        if (!File.Exists(filePath)) return;

        try
        {
            var xmlContent = File.ReadAllText(filePath);

            // 更新 Name
            if (!string.IsNullOrEmpty(NameTextBox.Text))
            {
                xmlContent = UpdateXmlTag(xmlContent, "Name", NameTextBox.Text);
                _model.Remark = NameTextBox.Text;
            }

            // 更新 Assembly
            if (!string.IsNullOrEmpty(AssemblyTextBox.Text))
            {
                xmlContent = UpdateXmlTag(xmlContent, "Assembly", AssemblyTextBox.Text);
            }

            // 更新 FullClassName
            if (!string.IsNullOrEmpty(ClassNameTextBox.Text))
            {
                xmlContent = UpdateXmlTag(xmlContent, "FullClassName", ClassNameTextBox.Text);
            }

            // 更新 VendorId
            xmlContent = UpdateXmlTag(xmlContent, "VendorId", VendorIdTextBox.Text);

            // 更新 VendorDescription
            xmlContent = UpdateXmlTag(xmlContent, "VendorDescription", VendorDescTextBox.Text);

            File.WriteAllText(filePath, xmlContent);

            // 更新模型中的信息
            _originalInfo.Name = NameTextBox.Text;
            _originalInfo.Assembly = AssemblyTextBox.Text;
            _originalInfo.FullClassName = ClassNameTextBox.Text;
            _originalInfo.VendorId = VendorIdTextBox.Text;
            _originalInfo.VendorDescription = VendorDescTextBox.Text;
        }
        catch
        {
            MessageBox.Show("保存文件失败，请检查文件权限", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            return before + value + after;
        }

        return xml;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
