using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace RAWSimO.Visualization.Avalonia.Controls
{
    /// <summary>
    /// A minimal modal message box (Avalonia does not ship one).
    /// </summary>
    public static class MessageDialog
    {
        public static Task Show(Window owner, string title, string message)
        {
            var ok = new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 80, IsDefault = true, IsCancel = true };
            var dialog = new Window
            {
                Title = title,
                Width = 560,
                SizeToContent = SizeToContent.Height,
                MaxHeight = 700,
                CanResize = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false,
                Content = new DockPanel
                {
                    Margin = new Thickness(16),
                    Children =
                    {
                        new Border { [DockPanel.DockProperty] = Dock.Bottom, Margin = new Thickness(0, 12, 0, 0), Child = ok },
                        new ScrollViewer
                        {
                            Content = new SelectableTextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                        },
                    },
                },
            };
            ok.Click += (_, _) => dialog.Close();
            return dialog.ShowDialog(owner);
        }
    }
}
