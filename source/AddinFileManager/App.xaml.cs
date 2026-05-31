using System.Windows;

namespace AddinFileManager;

public partial class App : Application
{
    public static bool IsDarkTheme { get; private set; }

    public static void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ApplyTheme(IsDarkTheme);
    }

    private static void ApplyTheme(bool isDark)
    {
        // 移除当前主题
        var oldTheme = isDark
            ? "pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml"
            : "pack://application:,,,/MahApps.Metro;component/Styles/Themes/Dark.Blue.xaml";

        var newTheme = isDark
            ? "pack://application:,,,/MahApps.Metro;component/Styles/Themes/Dark.Blue.xaml"
            : "pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml";

        // 更新资源字典
        var resources = Current.Resources.MergedDictionaries;
        for (int i = 0; i < resources.Count; i++)
        {
            if (resources[i].Source != null &&
                resources[i].Source.ToString().Contains("Themes/"))
            {
                resources[i] = new ResourceDictionary { Source = new System.Uri(newTheme) };
                break;
            }
        }
    }
}
