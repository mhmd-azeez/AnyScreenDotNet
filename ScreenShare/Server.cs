using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Forms;

namespace ScreenShare
{
    public class Server
    {
        private readonly string _path;
        private readonly System.Timers.Timer _timer;

        public Server(string path)
        {
            _path = path;
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += _timer_Elapsed;
        }

        public void Start()
        {
            if (Directory.Exists(_path) == false)
            {
                Directory.CreateDirectory(_path);
            }
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SaveScreen(Screen.PrimaryScreen.Bounds, _path);
        }

        public void SaveScreen(Rectangle bounds, string path)
        {
            try
            {
                Bitmap myImage = new Bitmap(bounds.Width, bounds.Height);
                Graphics gr1 = Graphics.FromImage(myImage);
                IntPtr dc1 = gr1.GetHdc();
                IntPtr dc2 = NativeMethods.GetWindowDC(NativeMethods.GetDesktopWindow());
                NativeMethods.BitBlt(dc1, bounds.X, bounds.Y, bounds.Width, bounds.Height, dc2, bounds.X, bounds.Y, 13369376);
                gr1.ReleaseHdc(dc1);
                var fileName = Path.Combine(path, Guid.NewGuid().ToString() + ".png");
                myImage.Save(fileName, ImageFormat.Png);
                myImage.Dispose();
            }
            catch { }
        }
    }

    internal class NativeMethods
    {
        [DllImport("user32.dll")]
        public extern static IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hwnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("gdi32.dll")]
        public static extern UInt64 BitBlt(IntPtr hDestDC, int x, int y,
           int nWidth, int nHeight, IntPtr hSrcDC,
           int xSrc, int ySrc, int dwRop);
    }
}
