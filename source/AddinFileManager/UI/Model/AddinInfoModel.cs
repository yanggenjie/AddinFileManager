using PropertyChanged;
using System.IO;
using System.Xml.Linq;

namespace AddinFileManager.UI.Model;

[AddINotifyPropertyChangedInterface]
public class AddinInfoModel
{
    /// <summary>
    /// 安装位置（全局/用户）
    /// </summary>
    public string InstallLocation { get; set; }

    /// <summary>
    /// 插件文件名
    /// </summary>
    public string AddinFileName { get; set; }

    /// <summary>
    /// 插件名称（从 .addin 文件解析）
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 批量操作时的选中状态
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsOn { get; set; }

    /// <summary>
    /// 文件完整路径
    /// </summary>
    public string FileFullPath { get; set; }

    /// <summary>
    /// 插件完整信息（用于详情显示）
    /// </summary>
    public AddinFullInfo FullInfo { get; private set; }

    /// <summary>
    /// 加载完整插件信息
    /// </summary>
    public AddinFullInfo LoadFullInfo()
    {
        if (FullInfo != null) return FullInfo;
        if (!File.Exists(FileFullPath)) return null;

        try
        {
            var doc = XDocument.Load(FileFullPath);
            var addinElement = doc.Element("RevitAddIns");
            if (addinElement == null) return null;

            var appElement = addinElement.Element("AddIn");
            if (appElement == null) return null;

            FullInfo = new AddinFullInfo
            {
                Name = appElement.Element("Name")?.Value?.Trim() ?? "",
                Assembly = appElement.Element("Assembly")?.Value?.Trim() ?? "",
                FullClassName = appElement.Element("FullClassName")?.Value?.Trim() ?? "",
                VendorId = appElement.Element("VendorId")?.Value?.Trim() ?? "",
                VendorDescription = appElement.Element("VendorDescription")?.Value?.Trim() ?? "",
                AddinType = appElement.Attribute("Type")?.Value ?? "",
            };
            return FullInfo;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// 插件完整信息
/// </summary>
public class AddinFullInfo
{
    public string Name { get; set; }
    public string Assembly { get; set; }
    public string FullClassName { get; set; }
    public string VendorId { get; set; }
    public string VendorDescription { get; set; }
    public string AddinType { get; set; }
}
