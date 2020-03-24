using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Timers;
using System.Windows.Forms;

namespace ScreenShare
{
    public class ScreenShareService
    {
        private readonly string _requestorId;
        private readonly IServer _server;
        private readonly System.Timers.Timer _timer;

        public ScreenShareService(TimeSpan interval, string requestorId, IServer server)
        {
            _timer = new System.Timers.Timer();
            _timer.Interval = interval.TotalMilliseconds;
            _timer.Elapsed += Timer_Elapsed;

            _requestorId = requestorId;
            _server = server;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var frame = SaveScreen(Screen.PrimaryScreen.Bounds);
            await _server.Publish(frame, _requestorId, ImageFormat.Jpeg);
        }

        public Stream SaveScreen(Rectangle bounds)
        {
            try
            {
                var stream = new MemoryStream();
                var scale = 1f;

                var scaledWidth = (int)(bounds.Width * scale);
                var scaledHeight = (int)(bounds.Height * scale);

                Bitmap myImage = new Bitmap(scaledWidth, scaledHeight);
                Graphics gr1 = Graphics.FromImage(myImage);

                IntPtr dc1 = gr1.GetHdc();
                IntPtr dc2 = NativeMethods.GetWindowDC(NativeMethods.GetDesktopWindow());

                NativeMethods.SetStretchBltMode(dc1, StretchBltMode.STRETCH_HALFTONE);

                NativeMethods.StretchBlt(dc1, bounds.X, bounds.Y, scaledWidth, scaledHeight,
                    dc2, bounds.X, bounds.Y, bounds.Width, bounds.Height, 13369376);
                gr1.ReleaseHdc(dc1);

                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

                // Create an Encoder object based on the GUID  
                // for the Quality parameter category.  
                Encoder myEncoder = Encoder.Quality;

                // Create an EncoderParameters object.  
                // An EncoderParameters object has an array of EncoderParameter  
                // objects. In this case, there is only one  
                // EncoderParameter object in the array.  
                EncoderParameters myEncoderParameters = new EncoderParameters(1);

                var quality = 80L;
                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, quality);
                myEncoderParameters.Param[0] = myEncoderParameter;
                myImage.Save(stream, jpgEncoder, myEncoderParameters);

                myImage.Dispose();
                gr1.Dispose();

                return stream;
            }
            catch { }
            return null;
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        internal void Start()
        {
            _timer.Start();
        }
    }
}
