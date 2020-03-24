using Newtonsoft.Json;
using SocketIOClient;
using SocketIOClient.Arguments;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ScreenShare
{
    public static class StreamExtensions
    {
        // https://stackoverflow.com/a/60110009/7003797
        public static string ConvertToBase64(this Stream stream)
        {
            var bytes = new byte[(int)stream.Length];

            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(bytes, 0, (int)stream.Length);

            return Convert.ToBase64String(bytes);
        }
    }

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

                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 80L);
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

    public class ScreenShareRequestedEventArgs : EventArgs
    {
        public ScreenShareRequestedEventArgs(string requestorId)
        {
            RequestorId = requestorId;
        }

        public string RequestorId { get; set; }
    }

    public class ConnectedToServerEventArgs : EventArgs
    {
        public ConnectedToServerEventArgs(string myClientId)
        {
            MyClientId = myClientId;
        }

        public string MyClientId { get; set; }
    }

    public class SocketIoServer : IServer
    {
        private readonly string _url;
        private readonly SocketIO _client;
        private string _id;
        private ScreenShareService _service;

        public SocketIoServer(string url)
        {
            _url = url;
            _client = new SocketIO(url)
            {
                // if server need some parameters, you can add to here
                //Parameters = new Dictionary<string, string>
                //{
                //    { "uid", "" },
                //    { "token", "" }
                //}
            };

            _client.On("welcome", WelcomeHandler);
            _client.On("requestScreenShare", RequestScreenShareHandler);
            _client.OnConnected += _client_OnConnected;
            _client.OnClosed += _client_OnClosed;
            _client.OnError += _client_OnError;
        }

        public event EventHandler<ScreenShareRequestedEventArgs> ScreenShareRequested;
        public event EventHandler<ConnectedToServerEventArgs> ConnectedToServer;

        private void RequestScreenShareHandler(ResponseArgs args)
        {
            ScreenShareRequested?.Invoke(this, new ScreenShareRequestedEventArgs(args.Text));
        }

        public async Task AcceptScreenShare(string requestorId)
        {
            await _client.EmitAsync("screenShareAccepted", requestorId);
            _service = new ScreenShareService(TimeSpan.FromMilliseconds(500), requestorId, this);
            _service.Start();
        }

        private void WelcomeHandler(ResponseArgs args)
        {
            _id = JsonConvert.DeserializeObject<string>(args.Text);
            ConnectedToServer?.Invoke(this, new ConnectedToServerEventArgs(_id));
            Debug.WriteLine(_id);
        }

        private void _client_OnError(ResponseArgs error)
        {
            Debug.WriteLine($"Error: {error.Text}");
        }

        private void _client_OnClosed(ServerCloseReason reason)
        {
            Debug.WriteLine($"Disconnected from {_url}. Reason: {Enum.GetName(typeof(ServerCloseReason), reason)}");
        }

        private void _client_OnConnected()
        {
            Debug.WriteLine($"Connected to {_url}");
        }

        public async Task Start()
        {
            await _client.ConnectAsync();
        }

        public async Task Publish(Stream stream, string requestorId, ImageFormat format)
        {

            var formatText = format == ImageFormat.Png ? "png" :
                             format == ImageFormat.Jpeg ? "jpeg" :
                             format == ImageFormat.Bmp ? "bmp" :
                             "jpeg";

            var frameText = $"data:image/{formatText};base64,{stream.ConvertToBase64()}";

            await _client.EmitAsync("frame", new
            {
                to = requestorId,
                image = frameText
            });
        }
    }
}
