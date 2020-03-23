using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ScreenShare
{
    public class Client
    {
        private readonly string _path;

        public Client(string path)
        {
            _path = path;
        }

        public void Start()
        {
            var watcher = new FileSystemWatcher(_path);
            watcher.EnableRaisingEvents = true;
            watcher.Filter = "*.png";
            watcher.Created += Watcher_Created;
        }

        public event EventHandler<BitmapImage> ImageReceived;

        private async void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            await Task.Delay(100);
            var path = Path.GetFullPath(e.FullPath);
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();

            ImageReceived?.Invoke(this, bitmap);
        }
    }
}
