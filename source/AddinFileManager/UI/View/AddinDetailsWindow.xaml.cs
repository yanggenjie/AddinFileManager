using AddinFileManager.Services;
using AddinFileManager.UI.Model;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace AddinFileManager.UI.View;

public partial class AddinDetailsWindow : Window
{
    private readonly AddinInfoModel _model;

    public AddinDetailsWindow(AddinInfoModel model)
    {
        InitializeComponent();
        _model = model;
        LoadData();
    }

    private void LoadData()
    {
        TitleName.Text = _model.Remark ?? _model.AddinFileName;

        FileNameText.Text = _model.AddinFileName;
        LocationText.Text = _model.InstallLocation;
        StatusText.Text = _model.IsOn ? "已启用" : "已禁用";
        StatusText.Foreground = _model.IsOn
            ? (Brush)new BrushConverter().ConvertFrom("#43A047")
            : (Brush)new BrushConverter().ConvertFrom("#E53935");
        FullPathText.Text = _model.FileFullPath;

        var fullInfo = _model.LoadFullInfo();
        if (fullInfo != null)
        {
            NameText.Text = fullInfo.Name;
            TypeText.Text = fullInfo.AddinType;
            AssemblyText.Text = fullInfo.Assembly;
            ClassNameText.Text = fullInfo.FullClassName;
            VendorIdText.Text = fullInfo.VendorId;
            VendorDescText.Text = fullInfo.VendorDescription;
        }
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

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}