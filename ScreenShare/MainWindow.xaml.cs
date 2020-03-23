using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace ScreenShare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Server _server;
        private const string StorageFolder = "storage";
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }


        private void btnServer_Click(object sender, RoutedEventArgs e)
        {
            _server = new Server(StorageFolder);
            _server.Start();
            btnServer.IsEnabled = false;
        }

        private void btnClient_Click(object sender, RoutedEventArgs e)
        {
            var window = new ClientWindow(StorageFolder);
            window.Show();
        }
    }
}
