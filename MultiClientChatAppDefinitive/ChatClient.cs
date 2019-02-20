using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MultiClientChatAppDefinitive
{
    public class ChatClient
    {
        public Guid ID { get; set; }
        public string Username { get; set; }
        public string IP { get; set; }
        public int PORT { get; set; }
        public int BUFFER_SIZE { get; set; }
        public TcpClient tcpClient { get; set; }
        private TcpClient Server;

        public bool isActive { get; set; }

        private Action<string> AddMessage;
        private Action ToggleButtonsDisconnected;
        private Action ToggleButtonsConnected;

        public ChatClient(Guid ID, string Username, TcpClient tcpClient)
        {
            this.ID = ID;
            this.Username = Username;
            this.tcpClient = tcpClient;

            IP = "127.0.0.1";
            PORT = 8000;
            BUFFER_SIZE = 1024;
            isActive = false;
        }

        public void SetDependencies(string IP, int PORT, int BUFFER_SIZE, Action<string> AddMessage, Action ToggleButtonsDisconnected, Action ToggleButtonsConnected)
        {
            this.IP = IP;
            this.PORT = PORT;
            this.BUFFER_SIZE = BUFFER_SIZE;
            this.AddMessage = AddMessage;
            this.ToggleButtonsDisconnected = ToggleButtonsDisconnected;
            this.ToggleButtonsConnected = ToggleButtonsConnected;
        }

        public async void Connect()
        {
            try
            {
                AddMessage("Connecting..");

                using (Server = new TcpClient(IP, PORT))
                {
                    StreamWriter writer = new StreamWriter(Server.GetStream(), Encoding.UTF32);
                    writer.AutoFlush = true;
                    writer.WriteLine(Username);

                    await Task.Run(() => ReceiveDataFromServer());
                }
            }
            catch
            {
                AddMessage("Connection refused.");
            }
        }

        private void ReceiveDataFromServer()
        {
            using (var stream = Server.GetStream())
            {
                AddMessage("Connected!");
                isActive = true;
                ToggleButtonsConnected();

                try
                {
                    while (true)
                    {
                        string receivedMessage = ListenForServerMessage(stream);

                        if (receivedMessage == "Server: exit")
                        {
                            AddMessage("Server has closed the connection.");
                            break;
                        }

                        if (!string.IsNullOrEmpty(receivedMessage))
                            AddMessage(receivedMessage);
                    }

                    CloseConnectionToServer();
                    isActive = false;
                }
                catch
                {
                    AddMessage("Server disconnected unexpectedly!");
                    CloseConnectionToServer();
                    isActive = false;
                }
            }
        }

        private string ListenForServerMessage(NetworkStream stream)
        {
            StreamReader sr = new StreamReader(stream, Encoding.UTF32, false, BUFFER_SIZE);
            return sr.ReadLine();
        }

        public void SendMessage(string message)
        {
            StreamWriter sr = new StreamWriter(Server.GetStream(), Encoding.UTF32);
            sr.AutoFlush = true;
            sr.WriteLine(message);
            
            if (message == "exit")
                CloseConnectionToServer();

            AddMessage("You: " + message);
        }

        private void CloseConnectionToServer()
        {
            Server.Close();
            Server.Dispose();

            AddMessage("Disconnected.");
            ToggleButtonsDisconnected();
            isActive = false;
        }
    }
}
