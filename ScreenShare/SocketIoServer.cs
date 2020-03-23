using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient;
using SocketIOClient.Arguments;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ScreenShare
{
    public class SocketIoServer
    {
        private readonly string _url;
        private readonly SocketIO _client;
        private string _id;
        private string _requestorId;

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

        private async void RequestScreenShareHandler(ResponseArgs args)
        {
            _requestorId = args.Text;
            await _client.EmitAsync("screenShareAccepted", _requestorId);

            var timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            Console.WriteLine("Request Screen share");
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var frame = SaveScreen(Screen.PrimaryScreen.Bounds);
            var frameText = $"data:image/png;base64,{ConvertToBase64(frame)}";

            await _client.EmitAsync("frame", new
            {
                to = _requestorId,
                image = frameText
            });

            Console.WriteLine("Frame sent");
        }

        public Stream SaveScreen(Rectangle bounds)
        {
            try
            {
                var stream = new MemoryStream();

                Bitmap myImage = new Bitmap(bounds.Width, bounds.Height);
                Graphics gr1 = Graphics.FromImage(myImage);
                IntPtr dc1 = gr1.GetHdc();
                IntPtr dc2 = NativeMethods.GetWindowDC(NativeMethods.GetDesktopWindow());
                NativeMethods.BitBlt(dc1, bounds.X, bounds.Y, bounds.Width, bounds.Height, dc2, bounds.X, bounds.Y, 13369376);
               // NativeMethods.BitBlt(dc1, bounds.X, bounds.Y, 100, 100, dc2, bounds.X, bounds.Y, 13369376);
                gr1.ReleaseHdc(dc1);
                myImage.Save(stream, ImageFormat.Png);
                myImage.Dispose();

                return stream;
            }
            catch { }
            return null;
        }

        private void WelcomeHandler(ResponseArgs args)
        {
            _id = JsonConvert.DeserializeObject<string>(args.Text);
            Debug.WriteLine(_id);
        }

        private void _client_OnError(SocketIOClient.Arguments.ResponseArgs error)
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

        public async Task Publish(Stream stream, string sessionId)
        {
            // https://backend.chawilka.com
            await _client.EmitAsync("publish", new
            {
                to = sessionId,
                image = ConvertToBase64(stream),
            });
        }

        // https://stackoverflow.com/a/60110009/7003797
        public static string ConvertToBase64(Stream stream)
        {
            var bytes = new byte[(int)stream.Length];

            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(bytes, 0, (int)stream.Length);

            return Convert.ToBase64String(bytes);
        }

    }
}
