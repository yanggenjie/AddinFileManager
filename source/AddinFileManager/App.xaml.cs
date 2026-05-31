using System.Windows;

namespace AddinFileManager;

public partial class App : Application
{
    public static bool IsDarkTheme { get; private set; }

    public static void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        // 主题切换功能暂时禁用，MahApps.Metro 2.x 需要更复杂的实现
        // 未来可以考虑使用 MahApps.Metro 主题管理器
    }
}