/*
 * This file is part of Libreccio Weather.
 *
 * Libreccio Weather is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by the Free
 * Software Foundation, version 3 of the License.
 *
 * Libreccio Weather is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
 * more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Libreccio Weather. If not, see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using Point = System.Drawing.Point;

namespace LibreccioWeather.Tray;

public partial class TrayPopup : Window
{
    private bool _closing;

    public TrayPopup()
    {
        InitializeComponent();
        Deactivated += (_, _) => ClosePopup();
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                ClosePopup();
            }
        };
    }

    public event EventHandler? OpenRequested;

    public void ShowAt(Point anchor)
    {
        Opacity = 0;
        Show();
        IntPtr handle = new WindowInteropHelper(this).Handle;
        // Move onto the target monitor before measuring with its DPI.
        SetWindowPos(handle, new IntPtr(-1), anchor.X, anchor.Y, 0, 0, 0x0001 | 0x0010);
        UpdateLayout();
        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        System.Drawing.Rectangle area = Forms.Screen.FromPoint(anchor).WorkingArea;
        MaxWidth = area.Width / dpi.DpiScaleX;
        MaxHeight = area.Height / dpi.DpiScaleY;
        UpdateLayout();
        int width = (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX);
        int height = (int)Math.Ceiling(ActualHeight * dpi.DpiScaleY);
        int x = Math.Clamp(anchor.X - width / 2, area.Left, Math.Max(area.Left, area.Right - width));
        int y = Math.Clamp(anchor.Y - height, area.Top, Math.Max(area.Top, area.Bottom - height));
        SetWindowPos(handle, new IntPtr(-1), x, y, 0, 0, 0x0001 | 0x0010);
        Opacity = 1;
        if (!Activate())
        {
            ClosePopup();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _closing = true;
        base.OnClosing(e);
    }

    private void ClosePopup()
    {
        if (!_closing)
        {
            Close();
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        ClosePopup();
    }

    private void OnOpenClick(object sender, RoutedEventArgs e)
    {
        OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
