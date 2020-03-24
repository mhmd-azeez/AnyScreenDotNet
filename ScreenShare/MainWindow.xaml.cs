using System;
using System.Diagnostics;
using System.Windows;

namespace ScreenShare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IServer _server;
        private const string StorageFolder = "storage";
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            btnServer.IsEnabled = false;
            _server = new SocketIoServer("https://backend.chawilka.com");
            _server.ConnectedToServer += _server_ConnectedToServer;
            _server.ScreenShareRequested += _server_ScreenShareRequested;
            await _server.Start();
        }

        private void _server_ScreenShareRequested(object sender, ScreenShareRequestedEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                var result = MessageBox.Show
                (
                    Application.Current.MainWindow,
                    $"Screen share requested by: '{e.RequestorId}'. Do you Accept it?",
                    "Share screen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    await _server.AcceptScreenShare(e.RequestorId);
                }
            });
        }

        private void Log(string text)
        {
            logTextBox.Text += text + Environment.NewLine;
        }

        private void _server_ConnectedToServer(object sender, ConnectedToServerEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Log($"Connected to server. My Id: {e.MyClientId}");
            });
        }

        private void btnClient_Click(object sender, RoutedEventArgs e)
        {
            var window = new ClientWindow(StorageFolder);
            window.Show();
        }
    }
}
