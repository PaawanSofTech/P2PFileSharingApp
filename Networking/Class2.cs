using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace P2PFileSharingApp.Networking
{
    public class Client
    {
        public static async Task<string[]> GetFileListAsync(string ip, int port)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(ip, port);
                using (var stream = client.GetStream())
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                using (var reader = new StreamReader(stream))
                {
                    writer.WriteLine("GET FILE LIST");
                    string response = await reader.ReadLineAsync();
                    return response?.Split(',') ?? Array.Empty<string>();
                }
            }
        }

        public static async Task DownloadFileAsync(string ip, int port, string fileName, string savePath)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(ip, port);
                using (var stream = client.GetStream())
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    writer.WriteLine($"DOWNLOAD {fileName}");
                    using (var fileStream = File.Create(savePath))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }
            }
        }

        public static async Task UploadFileAsync(string ip, int port, string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(ip, port);
                using (var stream = client.GetStream())
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    writer.WriteLine($"UPLOAD {fileName}");
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        await fileStream.CopyToAsync(stream);
                    }
                }
            }
        }

        public static async Task DeleteFileAsync(string ip, int port, string fileName)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(ip, port);
                using (var stream = client.GetStream())
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                using (var reader = new StreamReader(stream))
                {
                    writer.WriteLine($"DELETE {fileName}");
                    string response = await reader.ReadLineAsync();

                    if (response == "DELETE SUCCESS")
                    {
                        Console.WriteLine($"File {fileName} deleted successfully on server.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to delete file {fileName} on server.");
                    }
                }
            }
        }
    }
}
