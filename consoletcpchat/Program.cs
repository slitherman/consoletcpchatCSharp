using System.Net;
using System.Net.Sockets;
using System.Text;

namespace consoletcpchat
{
    internal class Program
    {
        private static TcpListener server;
        private static Dictionary<TcpClient, string> clients = new Dictionary<TcpClient, string>();

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
                    NetworkStream stream = client.GetStream();
                    // Receive the username from the client
                    byte[] usernameBuffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(usernameBuffer, 0, usernameBuffer.Length);
                    string username = Encoding.UTF8.GetString(usernameBuffer, 0, bytesRead);
                    // Store the username and client in the dictionary
                    clients.Add(client, username);

                    // Sending a welcome messages to all the connected users
                    string welcomeMessage = $"Welcome to the chat: {username}";
                    byte[] welcomeBytes = Encoding.UTF8.GetBytes(welcomeMessage);
                    await stream.WriteAsync(welcomeBytes, 0, welcomeBytes.Length); 
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
            string senderUsername = clients[client]; // Get the sender's username

            // Read and send messages
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break; // Client disconnected
                    }

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Checks if the message is a file request
                    // Broadcast the message to all connected clients, including the sender's username
                    foreach (var otherClient in clients)
                    {
                        NetworkStream otherStream = otherClient.Key.GetStream();
                        string responseMessage = $"{senderUsername} says: {msg}"; // Include sender's username
                        byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                        await otherStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                    // Split the input into parts
                    string[] parts = msg.Split(' ');

                    if (parts.Length == 4)
                    {
                        string fileName = parts[1];
                        int fileSize = int.Parse(parts[3]);

                        byte[] data = new byte[fileSize];
                        await stream.ReadAsync(data, 0, fileSize);

                        // Broadcast the file to all connected clients, except the sender
                        foreach (var otherClient in clients)
                        {
                            if (otherClient.Key != client)
                            {
                                NetworkStream otherStream = otherClient.Key.GetStream();

                                string fileTransferMessage = $"/file:{fileName}:{fileSize}";
                                byte[] msgBytes = Encoding.UTF8.GetBytes(fileTransferMessage);
                                await otherStream.WriteAsync(msgBytes, 0, msgBytes.Length);

                                await otherStream.WriteAsync(data, 0, data.Length);
                            }
                        }
                    }
                    else
                    {
                        // Broadcast the message to all connected clients, except the sender
                        foreach (var otherClient in clients)
                        {
                            if (otherClient.Key != client)
                            {
                                NetworkStream otherStream = otherClient.Key.GetStream();
                                byte[] responseBytes = Encoding.UTF8.GetBytes(msg);
                                await otherStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Used to perform non-blocking logging
                    await Console.Out.WriteLineAsync("An error occurred: " + ex.Message);
                }
            }

            // Remove the client from the list when they disconnect
            clients.Remove(client);
            client.Close();
        }


    }
}