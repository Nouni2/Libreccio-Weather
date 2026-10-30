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

using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using LibreccioWeather.Core.Localization;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Forms = System.Windows.Forms;
using Imaging = System.Windows.Media.Imaging;
using Media = System.Windows.Media;
using Point = System.Windows.Point;

namespace LibreccioWeather.Tray;

public static class TrayIconImages
{
    public static bool IsLightTaskbar => !Forms.SystemInformation.HighContrast
        && Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "SystemUsesLightTheme", 0) is 1;

    public static Icon Create(string? weatherIcon)
    {
        if (weatherIcon is null)
        {
            using Stream stream = Application.GetResourceStream(
                new Uri("pack://application:,,,/assets/icons/icon.ico", UriKind.Absolute)).Stream;
            using var icon = new Icon(stream, Forms.SystemInformation.SmallIconSize);
            return (Icon)icon.Clone();
        }

        using Stream imageStream = Application.GetResourceStream(new Uri(weatherIcon, UriKind.Absolute)).Stream;
        using var image = new Bitmap(imageStream);
        using var resized = new Bitmap(image, Forms.SystemInformation.SmallIconSize);
        return FromBitmap(resized);
    }

    public static Icon CreateTemperature(string temperature)
    {
        const int IconSize = 72;
        var family = new Media.FontFamily(new Uri("pack://application:,,,/"), "./assets/fonts/#Asap Condensed");
        var typeface = new Media.Typeface(family, System.Windows.FontStyles.Normal,
            System.Windows.FontWeights.Bold, System.Windows.FontStretches.Normal);
        Media.Brush foreground;
        if (Forms.SystemInformation.HighContrast)
        {
            Color color = SystemColors.WindowText;
            foreground = new Media.SolidColorBrush(Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
        else
        {
            foreground = IsLightTaskbar ? Media.Brushes.Black : Media.Brushes.White;
        }

        var text = new Media.FormattedText(temperature, Strings.Culture, System.Windows.FlowDirection.LeftToRight,
            typeface, IconSize, foreground, 1);
        System.Windows.Rect bounds = text.BuildGeometry(new Point(0, 0)).Bounds;
        double left = Math.Floor(bounds.Left);
        double right = Math.Ceiling(bounds.Right);
        double top = Math.Floor(bounds.Top);
        double bottom = Math.Ceiling(bounds.Bottom);
        int width = Math.Max(1, (int)right);
        var visual = new Media.DrawingVisual();
        Media.TextOptions.SetTextRenderingMode(visual, Media.TextRenderingMode.Grayscale);
        using (Media.DrawingContext drawing = visual.RenderOpen())
        {
            drawing.DrawText(text, new Point((width - (right - left)) / 2 - left,
                (IconSize + (bottom - top)) / 2 - bottom));
        }

        var bitmap = new Imaging.RenderTargetBitmap(width, IconSize, 96, 96, Media.PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new Imaging.PngBitmapEncoder();
        encoder.Frames.Add(Imaging.BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;
        using var image = new Bitmap(stream);
        Size size = Forms.SystemInformation.SmallIconSize;
        using var square = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(square))
        {
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            double scale = Math.Min(size.Width / (double)image.Width, size.Height / (double)image.Height);
            int targetWidth = Math.Max(1, (int)(image.Width * scale));
            int targetHeight = Math.Max(1, (int)(image.Height * scale));
            graphics.DrawImage(image, (size.Width - targetWidth) / 2, (size.Height - targetHeight) / 2, targetWidth, targetHeight);
        }

        return FromBitmap(square);
    }

    private static Icon FromBitmap(Bitmap bitmap)
    {
        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon borrowed = Icon.FromHandle(handle);
            return (Icon)borrowed.Clone();
        }
        finally
        {
            // GetHicon transfers ownership; cloning the managed icon does not release the original handle.
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);
}
