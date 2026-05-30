using System.Windows;

namespace AddinFileManager.UI.View
{
    public partial class MessageWindow : Window
    {
        public MessageWindow(string message, string title = "提示", MessageType type = MessageType.Info)
        {
            InitializeComponent();
            MessageTextBlock.Text = message;
            TitleTextBlock.Text = title;

            // 根据消息类型设置图标和颜色
            switch (type)
            {
                case MessageType.Error:
                    IconTextBlock.Text = ""; // Error icon
                    IconTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    OkButton.Background = System.Windows.Media.Brushes.Red;
                    break;
                case MessageType.Warning:
                    IconTextBlock.Text = ""; // Warning icon
                    IconTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                    OkButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 140, 0));
                    break;
                case MessageType.Success:
                    IconTextBlock.Text = ""; // Checkmark icon
                    IconTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                    OkButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 0));
                    break;
                default:
                    IconTextBlock.Text = ""; // Info icon
                    IconTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215));
                    OkButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215));
                    break;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Title_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }

    public enum MessageType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
