using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MultiClientChatAppDefinitive
{
    /// <summary>
    /// Class ChatClient
    /// </summary>
    public class ChatClient
    {
        // Guid for unique ID's
        public Guid ID { get; set; }
        public string Username { get; set; }
        public string IP { get; set; }
        public int PORT { get; set; }
        public int BUFFER_SIZE { get; set; }

        // tcpClient for when the Server uses a list of these objects
        public TcpClient tcpClient { get; set; }

        // Server client for when the programme acts like a connecting client
        private TcpClient Server;

        // Boolean for checking the status of the client and programme
        public bool isActive { get; set; }
        public bool disconnecting { get; set; }

        // Actions for updating buttons and UI outside of this class
        private Action<string> AddMessage;
        private Action ToggleButtonsDisconnected;
        private Action ToggleButtonsConnected;

        /// <summary>
        /// Constructor -> ID, username and clients are given. Also set default values for IP, PORT, BUFFER_SIZE and isActive
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Username"></param>
        /// <param name="tcpClient"></param>
        public ChatClient(Guid ID, string Username, TcpClient tcpClient)
        {
            this.ID = ID;
            this.Username = Username;
            this.tcpClient = tcpClient;

            IP = "127.0.0.1";
            PORT = 8000;
            BUFFER_SIZE = 1024;
            isActive = false;
            disconnecting = false;
        }

        /// <summary>
        /// If the programme acts like a client, addiotional dependencies are needed. MainWindow will supply these addiontional properties in this method.
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="PORT"></param>
        /// <param name="BUFFER_SIZE"></param>
        /// <param name="AddMessage"></param>
        /// <param name="ToggleButtonsDisconnected"></param>
        /// <param name="ToggleButtonsConnected"></param>
        public void SetDependencies(string IP, int PORT, int BUFFER_SIZE, Action<string> AddMessage, Action ToggleButtonsDisconnected, Action ToggleButtonsConnected)
        {
            this.IP = IP;
            this.PORT = PORT;
            this.BUFFER_SIZE = BUFFER_SIZE;

            // Actions for updating UI
            this.AddMessage = AddMessage;
            this.ToggleButtonsDisconnected = ToggleButtonsDisconnected;
            this.ToggleButtonsConnected = ToggleButtonsConnected;
        }

        /// <summary>
        /// Initiate connecting to Server
        /// </summary>
        public async void Connect()
        {
            // Try connecting to a new server instance, if fails, display error message and update the buttons
            try
            {
                AddMessage("Connecting..");
                ToggleButtonsConnected();

                // Use new server instance
                using (Server = new TcpClient(IP, PORT))
                {
                    StreamWriter writer = new StreamWriter(Server.GetStream(), Encoding.UTF32);
                    writer.AutoFlush = true;
                    writer.WriteLine(Username);

                    // Run ReceiveData async
                    await Task.Run(() => ReceiveDataFromServer());
                }
            }
            catch
            {
                ToggleButtonsDisconnected();
                AddMessage("Connection refused.");
            }
        }

        /// <summary>
        /// Listen for data received from Server
        /// </summary>
        private void ReceiveDataFromServer()
        {
            // Use the stream from the Server
            using (var stream = Server.GetStream())
            {
                // Update UI for user and update status of client
                AddMessage("Connected!");
                AddMessage("Type `exit` to disconnect.");
                isActive = true;

                // Try to receive messages, if fails; Server must be disconnecting, so we will also disconnect and update status.
                try
                {
                    // While true, keep listening for messages.
                    while (true)
                    {
                        // Get the message 
                        string receivedMessage = ListenForServerMessage(stream);

                        // If message equals 'exit', we also write back 'exit' and break out of the loop to stop listening.
                        if (receivedMessage == "Server: exit")
                        {
                            StreamWriter sw = new StreamWriter(stream, Encoding.UTF32);
                            sw.AutoFlush = true;
                            sw.WriteLine("exit");

                            AddMessage("Server has closed the connection.");
                            break;
                        }

                        // If message is not empty, display message in UI
                        if (!string.IsNullOrEmpty(receivedMessage))
                            AddMessage(receivedMessage);
                    }
                    // Close the connection, if not already disconnecting, and update status
                    if (!disconnecting) 
                        CloseConnectionToServer();

                    isActive = false;
                }
                catch
                {
                    // if not already disconnecting, server closed the connection.
                    if (!disconnecting)
                    {
                        AddMessage("Server disconnected unexpectedly!");
                        CloseConnectionToServer();
                    }

                    isActive = false;
                }
            }
        }

        /// <summary>
        /// Listen for messages from Server
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private string ListenForServerMessage(NetworkStream stream)
        {
            // Init a new streamReader to listen for messages from server stream. sr.ReadLine() is blocking, so will wait for a message to be received.
            StreamReader sr = new StreamReader(stream, Encoding.UTF32, false, BUFFER_SIZE);
            return sr.ReadLine();
        }

        /// <summary>
        /// Send a message to the server stream
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            // Init a new streamwriter to send message to server
            StreamWriter sr = new StreamWriter(Server.GetStream(), Encoding.UTF32);
            sr.AutoFlush = true;
            sr.WriteLine(message);
            
            // if we write exit, we want to close connections.
            if (message == "exit")
            {
                disconnecting = true;
                CloseConnectionToServer();
                disconnecting = false;
            }
            else
            {
                AddMessage("You: " + message);
            }
        }

        /// <summary>
        /// Close the connection to the server
        /// </summary>
        private void CloseConnectionToServer()
        {
            // update status, close and dispose the server instance, and update UI correspondingly.
            isActive = false;
            Server.Close();
            Server.Dispose();

            AddMessage("Disconnected.");
            ToggleButtonsDisconnected();
        }
    }
}
