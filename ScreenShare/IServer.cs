using System.IO;
using System.Threading.Tasks;

namespace ScreenShare
{
    public interface IServer
    {
        Task Start();
        Task Publish(Stream stream);
    }
}