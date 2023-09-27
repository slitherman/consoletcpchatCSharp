using System.Net;
using System.Net.Sockets;
using System.Text;

namespace consoletcpchat
{
    internal class Program
    {
        private static TcpListener server;
        private static List<TcpClient> clients = new List<TcpClient>();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Chat Server is running...");
            server = new TcpListener(IPAddress.Any, 6666); // Choose your desired port number
            server.Start();

            try
            {
                while (true)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    clients.Add(client);

                    // Handle client communication on a separate thread
                   await Task.Run(() => HandleClient(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            finally
            {
                server.Stop();
            }
        }

        private static async void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            // Read and send messages
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) 
                    {
                        break; //Client disconnected
                    }

                    string msg = Encoding.UTF8.GetString(buffer,0, bytesRead);  

                    //checks if the message is a file request
                    if(msg.StartsWith("/file"))
                    {

                        string[] parts = msg.Split(':');
                       if(parts.Length == 3)
                        {
                            string fileName = parts[1];
                            int fileSize = int.Parse(parts[2]);
                            byte[] data = new byte[fileSize];
                            await stream.ReadAsync(data, 0, fileSize);  

                            foreach (var otherClients in clients)
                            {
                                if(otherClients != client)
                                {
                                    NetworkStream otherStream = otherClients.GetStream();

                                    string fileTransferMessage = $"/file{fileName}:{fileSize}";
                                    byte[] msgBytes = Encoding.UTF8.GetBytes(fileTransferMessage);
                                    await otherStream.WriteAsync(msgBytes, 0, msgBytes.Length);

                                    await otherStream.WriteAsync(data, 0, data.Length);
                                        
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var otherClient in clients)
                        {
                            if (otherClient != client)
                            {
                                NetworkStream otherStream = otherClient.GetStream();
                                byte[] responseBytes = Encoding.UTF8.GetBytes(msg);
                                await otherStream.WriteAsync(responseBytes, 0, responseBytes.Length);

                            }
                        }
                    }


                }
                catch(Exception ex) 
                {
                    //used to perform non blocking logging 
                    await Console.Out.WriteLineAsync("An error occurred: " + ex.Message);
                }

                
            }
            // Remove the client from the list when they disconnect
            clients.Remove(client);
            client.Close();
        }

    }
}