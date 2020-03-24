using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace ScreenShare
{
    public interface IServer
    {
        event EventHandler<ScreenShareRequestedEventArgs> ScreenShareRequested;
        event EventHandler<ConnectedToServerEventArgs> ConnectedToServer;

        Task Start();
        Task Publish(Stream stream, string requestorId, ImageFormat format);
        Task AcceptScreenShare(string requestorId);
    }
}