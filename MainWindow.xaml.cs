using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using P2PFileSharingApp.Models;
using P2PFileSharingApp.Networking;

namespace P2PFileSharingApp
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<FileItem> _files = new ObservableCollection<FileItem>();
        private Peer _peer;

        public MainWindow()
        {
            InitializeComponent();

            // Bind the file list to the ObservableCollection
            FileListView.ItemsSource = _files;

            // Start the peer server with a dynamic port
            _peer = new Peer(0); // 0 allows dynamic port assignment
            _peer.Start();

            // Display the assigned port in a message box and update UI
            AssignedPortTextBox.Text = $"Server running on port: {_peer.Port}";
            MessageBox.Show($"Server started on port {_peer.Port}");

            // Refresh the local shared folder
            RefreshSharedFolder();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string ip = IpTextBox.Text;
            int port;

            if (string.IsNullOrEmpty(ip) || !int.TryParse(PortTextBox.Text, out port))
            {
                MessageBox.Show("Please enter a valid IP address and port.");
                return;
            }

            try
            {
                string[] files = await Client.GetFileListAsync(ip, port);
                _files.Clear();
                foreach (var file in files)
                {
                    _files.Add(new FileItem { FileName = Path.GetFileName(file), FileSize = "Unknown" });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to peer: {ex.Message}");
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            string ip = IpTextBox.Text;
            int port;

            if (string.IsNullOrEmpty(ip) || !int.TryParse(PortTextBox.Text, out port))
            {
                MessageBox.Show("Please enter a valid IP address and port.");
                return;
            }

            try
            {
                string[] files = await Client.GetFileListAsync(ip, port);
                _files.Clear();
                foreach (var file in files)
                {
                    _files.Add(new FileItem { FileName = Path.GetFileName(file), FileSize = "Unknown" });
                }
                MessageBox.Show("Remote file list refreshed successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing remote file list: {ex.Message}");
            }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                string ip = IpTextBox.Text;
                int port;

                if (string.IsNullOrEmpty(ip) || !int.TryParse(PortTextBox.Text, out port))
                {
                    MessageBox.Show("Please enter a valid IP address and port.");
                    return;
                }

                try
                {
                    await Client.UploadFileAsync(ip, port, filePath);
                    MessageBox.Show("File uploaded successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error uploading file: {ex.Message}");
                }
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileItem selectedFile)
            {
                string ip = IpTextBox.Text;
                int port;

                if (string.IsNullOrEmpty(ip) || !int.TryParse(PortTextBox.Text, out port))
                {
                    MessageBox.Show("Please enter a valid IP address and port.");
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = selectedFile.FileName
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        await Client.DownloadFileAsync(ip, port, selectedFile.FileName, dialog.FileName);
                        MessageBox.Show("File downloaded successfully!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error downloading file: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a file to download.");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileItem selectedFile)
            {
                string sharedFolder = "SharedFolder";
                string filePath = Path.Combine(sharedFolder, selectedFile.FileName);

                var result = MessageBox.Show($"Are you sure you want to delete '{selectedFile.FileName}'?",
                                             "Delete File",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            RefreshSharedFolder();
                            MessageBox.Show($"File '{selectedFile.FileName}' deleted successfully!");
                        }
                        else
                        {
                            MessageBox.Show($"File '{selectedFile.FileName}' does not exist.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting file: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a file to delete.");
            }
        }

        private void RefreshSharedFolder()
        {
            string sharedFolder = "SharedFolder";

            if (Directory.Exists(sharedFolder))
            {
                var files = Directory.GetFiles(sharedFolder);
                _files.Clear();
                foreach (var file in files)
                {
                    _files.Add(new FileItem { FileName = Path.GetFileName(file), FileSize = "Unknown" });
                }
            }
            else
            {
                MessageBox.Show("Shared folder does not exist.");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _peer?.Stop();
            base.OnClosing(e);
        }
    }
}
