using System.Windows;
using ControlzEx.Theming;

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
        var theme = isDark ? "Dark.Blue" : "Light.Blue";
        ThemeManager.Current.ChangeTheme(Current, theme);
    }
}
