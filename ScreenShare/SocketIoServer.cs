using Newtonsoft.Json;
using SocketIOClient;
using SocketIOClient.Arguments;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace ScreenShare
{

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
            var interval = TimeSpan.FromMilliseconds(500);
            _service = new ScreenShareService(interval, requestorId, this);
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
