using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenShare
{
    /// <summary>
    /// Interaction logic for Client.xaml
    /// </summary>
    public partial class ClientWindow : Window
    {
        private LocalClient _client;

        public ClientWindow(string path)
        {
            InitializeComponent();
            _client = new LocalClient(path);
            _client.ImageReceived += _client_ImageReceived;
            _client.Start();
        }

        private void _client_ImageReceived(object sender, System.Windows.Media.Imaging.BitmapImage e)
        {
            Dispatcher.Invoke(() =>
            {
                image.Source = e;
            });
        }
    }
}
