// TODO This can probably be remove
using Frotz;
using System.Drawing;
using System.IO;

namespace WPFMachine.Absolute;

internal static class ScaleImages
{
    internal static byte[] Scale(byte[] imgData, int scale)
    {
        var ms = new MemoryStream(imgData);

        using var img = Image.FromStream(ms);
        using var bmp = new Bitmap(img.Width * scale, img.Height * scale);

        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

        g.DrawImage(img, new Rectangle(0, 0, bmp.Width, bmp.Height),
            new Rectangle(0, 0, img.Width, img.Height),
            GraphicsUnit.Pixel);

        ms.Dispose();
        ms = OS.StreamManger.GetStream("ScaleImages.Scale");

        try
        {
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }
        finally
        {
            ms.Dispose();
        }
    }
}
