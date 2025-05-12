using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace P2PFileSharingApp.Networking
{
    public class Peer
    {
        private TcpListener _listener;
        private int _port;

        public int Port => _port; // Expose the dynamically assigned port

        public Peer(int port = 0)
        {
            _port = port;
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            _listener.Start();
            _port = ((IPEndPoint)_listener.LocalEndpoint).Port; // Get the assigned port
            Console.WriteLine($"Server started on port {_port}");
            _ = Task.Run(() => ListenForConnections());
        }

        public void Stop()
        {
            _listener?.Stop(); // Stop the TCP listener
            Console.WriteLine("Server stopped.");
        }

        private async Task ListenForConnections()
        {
            while (true)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            try
            {
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    string request = await reader.ReadLineAsync();
                    Console.WriteLine($"Request received: {request}");

                    if (request == "GET FILE LIST")
                    {
                        var files = Directory.GetFiles("SharedFolder");
                        await writer.WriteLineAsync(string.Join(",", files));
                    }
                    else if (request.StartsWith("DOWNLOAD"))
                    {
                        string fileName = request.Split(' ')[1];
                        await SendFile(fileName, stream);
                    }
                    else if (request.StartsWith("UPLOAD"))
                    {
                        string fileName = request.Split(' ')[1];
                        await ReceiveFile(fileName, stream);
                    }
                    else if (request.StartsWith("DELETE"))
                    {
                        string fileName = request.Split(' ')[1];
                        DeleteFile(fileName, writer);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
        }

        private async Task SendFile(string fileName, NetworkStream stream)
        {
            string filePath = Path.Combine("SharedFolder", fileName);
            if (File.Exists(filePath))
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
                Console.WriteLine($"File {fileName} sent successfully.");
            }
            else
            {
                Console.WriteLine($"File not found: {fileName}");
            }
        }

        private async Task ReceiveFile(string fileName, NetworkStream stream)
        {
            string filePath = Path.Combine("SharedFolder", fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await stream.CopyToAsync(fileStream);
            }
            Console.WriteLine($"File {fileName} received and saved successfully.");
        }

        private void DeleteFile(string fileName, StreamWriter writer)
        {
            string filePath = Path.Combine("SharedFolder", fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                writer.WriteLine("DELETE SUCCESS");
                Console.WriteLine($"File {fileName} deleted successfully.");
            }
            else
            {
                writer.WriteLine("DELETE FAILED");
                Console.WriteLine($"File {fileName} not found for deletion.");
            }
        }
    }
}
