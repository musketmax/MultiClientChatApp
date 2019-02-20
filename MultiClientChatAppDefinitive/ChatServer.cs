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
    public class ChatServer
    {
        public int PORT { get; set; }
        public int BUFFER_SIZE { get; set; }
        public TcpClient tcpClient { get; set; }

        private TcpListener Listener;
        public List<ChatClient> clients;
        private Action<string> AddMessage;
        private Action ToggleServerActiveUI;

        public bool isServerActive { get; set; }

        public ChatServer(int PORT, int BUFFER_SIZE, Action<string> AddMessage, Action ToggleServerActiveUI)
        {
            this.PORT = PORT;
            this.BUFFER_SIZE = BUFFER_SIZE;
            this.AddMessage = AddMessage;
            this.ToggleServerActiveUI = ToggleServerActiveUI;

            isServerActive = false;
            clients = new List<ChatClient>();
        }

        public void ListenForClients()
        {
            try
            {
                isServerActive = true;
                ToggleServerActiveUI();
                Listener = new TcpListener(IPAddress.Any, PORT);
                Listener.Start();
                Listener.BeginAcceptTcpClient(OnAcceptTcpClient, Listener);
            }
            catch (Exception exception)
            {
                AddMessage(exception.Message);
            }
        }

        private async void OnAcceptTcpClient(IAsyncResult result)
        {
            if (isServerActive)
            {
                using (var connectedClient = Listener.EndAcceptTcpClient(result))
                {
                    try
                    {
                        if (isServerActive)
                        {
                            StreamReader reader = new StreamReader(connectedClient.GetStream(), Encoding.UTF32, false, BUFFER_SIZE);
                            string username = reader.ReadLine();
                            ChatClient newClient = CreateNewClient(username, connectedClient);
                            Listener.BeginAcceptTcpClient(OnAcceptTcpClient, Listener);
                            clients.Add(newClient);

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

        private ChatClient CreateNewClient(string username, TcpClient connectedClient)
        {
            ChatClient newClient = new ChatClient(new Guid(), username, connectedClient);
            return newClient;
        }

        private void ReceiveDataFromClient(ChatClient chatClient)
        {
            using (var stream = chatClient.tcpClient.GetStream())
            {
                try
                {
                    SendMessageToAllClients($"{chatClient.Username} joined the chat!", chatClient);

                    while (true)
                    {
                        string receivedMessage = ListenForClientMessage(stream);

                        if (receivedMessage == "exit")
                        {
                            SendMessageToAllClients($"{chatClient.Username} left the chat!", chatClient);
                            break;
                        }

                        if (!string.IsNullOrEmpty(receivedMessage))
                            SendMessageToAllClients(receivedMessage, chatClient);
                    }

                    ChatClient clientToRemove = clients.First(c => c.ID == chatClient.ID);
                    clients.Remove(clientToRemove);
                    CloseConnectionToClient(chatClient);
                }
                catch
                {
                    ChatClient clientToRemove = clients.First(c => c.ID == chatClient.ID);
                    clients.Remove(clientToRemove);
                    CloseConnectionToClient(chatClient);
                    SendMessageToAllClients($"{chatClient.Username} disconnected unexpectedly!", chatClient);
                }
            }
        }

        private string ListenForClientMessage(NetworkStream stream)
        {
            StreamReader sr = new StreamReader(stream, Encoding.UTF32, false, BUFFER_SIZE);
            return sr.ReadLine();
        }

        private void SendMessageToAllClients(string message, ChatClient sender)
        {
            foreach (ChatClient chatClient in clients.Where(c => c != sender))
            {
                StreamWriter sr = new StreamWriter(chatClient.tcpClient.GetStream(), Encoding.UTF32);
                sr.AutoFlush = true;
                sr.WriteLine($"{sender.Username}: " + message);
            }

            AddMessage($"{sender.Username}: " + message);
        }

        public void SendMessage(string message)
        {
           
                foreach (ChatClient chatClient in clients)
                {
                    StreamWriter sr = new StreamWriter(chatClient.tcpClient.GetStream(), Encoding.UTF32);
                    sr.AutoFlush = true;
                    sr.WriteLine("Server: " + message);
                }

                if (message != "exit")
                {
                    AddMessage("You: " + message);
                }
            
        }

        private void CloseConnectionToClient(ChatClient chatClient)
        {
            chatClient.tcpClient.Close();
            chatClient.tcpClient.Dispose();
        }

        public void CloseConnections()
        {
            isServerActive = false;
            SendMessage("exit");
            ToggleServerActiveUI();
            AddMessage("You have disconnected succesfully.");

            Listener.Stop();
            Listener = null;
        }
    }
}
