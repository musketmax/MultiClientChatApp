using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MultiClientChatAppDefinitive
{
    /// <summary>
    /// Class ChatServer
    /// </summary>
    public class ChatServer
    {
        public int PORT { get; set; }
        public int BUFFER_SIZE { get; set; }

        // Listener for listening for clients
        private TcpListener Listener;
        // List of connected clients
        public List<ChatClient> clients;

        // Actions to update UI
        private Action<string> AddMessage;
        private Action ToggleServerActiveUI;

        // Booleans to indicate status
        public bool isServerActive { get; set; }
        private bool disconnecting { get; set; }

        /// <summary>
        /// Contructor, Sever needs port, buffer_size and a few actions to update the UI. Also set default values.
        /// </summary>
        /// <param name="PORT"></param>
        /// <param name="BUFFER_SIZE"></param>
        /// <param name="AddMessage"></param>
        /// <param name="ToggleServerActiveUI"></param>
        public ChatServer(int PORT, int BUFFER_SIZE, Action<string> AddMessage, Action ToggleServerActiveUI)
        {
            this.PORT = PORT;
            this.BUFFER_SIZE = BUFFER_SIZE;
            this.AddMessage = AddMessage;
            this.ToggleServerActiveUI = ToggleServerActiveUI;

            isServerActive = false;
            clients = new List<ChatClient>();
            disconnecting = false;
        }

        /// <summary>
        /// Initiate listening for clients
        /// </summary>
        public void ListenForClients()
        {
            // Try to init a new listener and set status. Give callback to listener for when a client connects.
            try
            {
                isServerActive = true;
                ToggleServerActiveUI();
                Listener = new TcpListener(IPAddress.Any, PORT);
                Listener.Start();
                Listener.BeginAcceptTcpClient(OnAcceptTcpClient, Listener);
            }
            catch
            {
                // If the Try fails, another server is most probably already running.
                AddMessage("Another Server is already running on this IP Address and Port.");
            }
        }

        /// <summary>
        /// Callback for listening for clients
        /// </summary>
        /// <param name="result"></param>
        private async void OnAcceptTcpClient(IAsyncResult result)
        {
            // Only use the tcpclient if server is still active (prevents crashes)
            if (isServerActive)
            {
                // Use the TcpClient instance just returned from the listener
                using (var connectedClient = Listener.EndAcceptTcpClient(result))
                {
                    // Try to, if server is active, read out username, create a new user with username, add to clients list, and begin listening for messages.
                    try
                    {
                        if (isServerActive)
                        {
                            StreamReader reader = new StreamReader(connectedClient.GetStream(), Encoding.UTF32, false, BUFFER_SIZE);
                            string username = reader.ReadLine();
                            ChatClient newClient = CreateNewClient(username, connectedClient);
                            Listener.BeginAcceptTcpClient(OnAcceptTcpClient, Listener);
                            clients.Add(newClient);

                            // Every client runs on own Task
                            await Task.Run(() => ReceiveDataFromClient(newClient));
                        }
                    }
                    catch
                    {
                        // Do nothing with caught exception
                    }
                }
            }
        }

        /// <summary>
        /// Create a new ChatClient instance
        /// </summary>
        /// <param name="username"></param>
        /// <param name="connectedClient"></param>
        /// <returns></returns>
        private ChatClient CreateNewClient(string username, TcpClient connectedClient)
        {
            ChatClient newClient = new ChatClient(new Guid(), username, connectedClient);
            return newClient;
        }

        /// <summary>
        /// Listen for client messages
        /// </summary>
        /// <param name="chatClient"></param>
        private void ReceiveDataFromClient(ChatClient chatClient)
        {
            // Use the stream given by new client
            using (var stream = chatClient.tcpClient.GetStream())
            {
                // Try to receive message from client, if fails; client must have disconnected, so we will also close the connection
                try
                {
                    // Tell everyone in the chat that a new user has connected
                    SendMessageToAllClients($"{chatClient.Username} joined the chat!", chatClient);

                    // While true, receive messages from client.
                    while (true)
                    {
                        // Receive message
                        string receivedMessage = ListenForClientMessage(stream);

                        // If message is 'exit', we tell everyone in the chat that the client has left, and we break the loop
                        if (receivedMessage == "exit")
                        {
                            SendMessageToAllClients($"{chatClient.Username} left the chat!", chatClient);
                            break;
                        }

                        // If message is not empty, send the message to all our clients
                        if (!string.IsNullOrEmpty(receivedMessage))
                            SendMessageToAllClients(receivedMessage, chatClient);
                    }

                    // If out of loop, client is no longer with us (RIP), so we remove him from the list and close connections.
                    ChatClient clientToRemove = clients.First(c => c == chatClient);
                    clients.Remove(clientToRemove);
                    CloseConnectionToClient(chatClient);

                    // If this client was the last one standing and we were in the process of disconnecting, we can safely disconnect everyone.
                    if (clients.Count < 1 && disconnecting)
                        CloseConnections();
                }
                catch
                {
                    // If this code executes, the client has force disconnected and we should close the connection immediately.
                    ChatClient clientToRemove = clients.First(c => c == chatClient);
                    clients.Remove(clientToRemove);
                    CloseConnectionToClient(chatClient);
                    SendMessageToAllClients($"{chatClient.Username} disconnected unexpectedly!", chatClient);

                    // If we were disconnecting, close up everything
                    if (clients.Count < 1 && disconnecting)
                        CloseConnections();
                }
            }
        }

        /// <summary>
        /// Listen for the sent message by client
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private string ListenForClientMessage(NetworkStream stream)
        {
            StreamReader sr = new StreamReader(stream, Encoding.UTF32, false, BUFFER_SIZE);
            return sr.ReadLine();
        }

        /// <summary>
        /// Send a message to all our clients except the one who sent it
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sender"></param>
        private void SendMessageToAllClients(string message, ChatClient sender)
        {
            // Iterate through every client (except the sender), and send them the message with corresponding username.
            foreach (ChatClient chatClient in clients.Where(c => c != sender))
            {
                StreamWriter sr = new StreamWriter(chatClient.tcpClient.GetStream(), Encoding.UTF32);
                sr.AutoFlush = true;
                sr.WriteLine($"{sender.Username}: " + message);
            }

            // Also update our own UI.
            AddMessage($"{sender.Username}: " + message);
        }

        /// <summary>
        /// Send message ordinarily (when send button clicked)
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            // For everyone of our clients, send the message and tell them it's from us
            foreach (ChatClient chatClient in clients)
            {
                StreamWriter sr = new StreamWriter(chatClient.tcpClient.GetStream(), Encoding.UTF32);
                sr.AutoFlush = true;
                sr.WriteLine("Server: " + message);
            }

            // If we wrote 'exit', we probably want to disconnect, so disconnecting is true.
            if (message != "exit")
            {
                AddMessage("You: " + message);
            }
            else
            {
                AddMessage("Disconnecting...");
                disconnecting = true;
            }
        }

        /// <summary>
        /// Close and cleanup every connection to the particular client.
        /// </summary>
        /// <param name="chatClient"></param>
        private void CloseConnectionToClient(ChatClient chatClient)
        {
            chatClient.tcpClient.Close();
            chatClient.tcpClient.Dispose();
        }

        /// <summary>
        /// Close all our connections and give up our listener. We want to fully cleanup, so everything is thrown away.
        /// </summary>
        public void CloseConnections()
        {
            // Update our statuses, send message to all our clients to say goodbye, toggle UI and destroy listener. Clients are wiped also.
            isServerActive = false;
            disconnecting = false;
            SendMessage("exit");
            ToggleServerActiveUI();
            AddMessage("You have disconnected succesfully.");

            Listener.Stop();
            Listener = null;
            clients = new List<ChatClient>();
        }
    }
}
