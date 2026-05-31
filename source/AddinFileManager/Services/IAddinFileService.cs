using AddinFileManager.UI.Model;
using System.Collections.Generic;

namespace AddinFileManager.Services;

/// <summary>
/// 插件文件服务接口
/// </summary>
public interface IAddinFileService
{
    /// <summary>
    /// 获取指定版本的插件列表
    /// </summary>
    IEnumerable<AddinInfoModel> GetAddinFiles(string version);

    /// <summary>
    /// 切换插件启用状态
    /// </summary>
    bool ToggleAddin(AddinInfoModel model, bool enable);

    /// <summary>
    /// 删除插件文件
    /// </summary>
    bool DeleteAddin(AddinInfoModel model);

    /// <summary>
    /// 打开插件所在目录
    /// </summary>
    void OpenFolder(string fileFullPath);
}
