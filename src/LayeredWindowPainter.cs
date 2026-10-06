using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DevMonitor
{
    internal static class LayeredWindowPainter
    {
        private const int UlwAlpha = 0x02;
        private const byte AcSrcOver = 0x00;
        private const byte AcSrcAlpha = 0x01;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeSize
        {
            public int Width;
            public int Height;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BlendFunction
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destinationDc, ref NativePoint destination,
            ref NativeSize size, IntPtr sourceDc, ref NativePoint source, int colorKey, ref BlendFunction blend, int flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr window);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr window, IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr dc, IntPtr gdiObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr gdiObject);

        public static void Paint(IntPtr window, Point location, Bitmap bitmap)
        {
            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memoryDc = CreateCompatibleDC(screenDc);
            IntPtr bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
            IntPtr previousBitmap = SelectObject(memoryDc, bitmapHandle);
            try
            {
                var destination = new NativePoint { X = location.X, Y = location.Y };
                var size = new NativeSize { Width = bitmap.Width, Height = bitmap.Height };
                var source = new NativePoint();
                var blend = new BlendFunction
                {
                    BlendOp = AcSrcOver,
                    SourceConstantAlpha = 255,
                    AlphaFormat = AcSrcAlpha
                };
                UpdateLayeredWindow(window, screenDc, ref destination, ref size, memoryDc, ref source, 0, ref blend, UlwAlpha);
            }
            finally
            {
                SelectObject(memoryDc, previousBitmap);
                DeleteObject(bitmapHandle);
                DeleteDC(memoryDc);
                ReleaseDC(IntPtr.Zero, screenDc);
            }
        }
    }
}
