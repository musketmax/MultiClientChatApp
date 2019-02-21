using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MultiClientChatAppDefinitive
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// ABANDON ALL HOPE, YE WHO ENTERS HERE
    /// </summary>
    public partial class MainWindow : Window
    {
        // Server and Client instances
        private ChatServer chatServer;
        private ChatClient chatClient;

        // Booleans for valid fields
        private bool isValidPort = false;
        private bool isValidBufferSize = false;
        private bool isValidIP = false;

        /// <summary>
        /// Programme init
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            // Check the validity of fields on startup
            CheckValidity();

            // Set the send message and stop button disabled on startup
            btnStop.IsEnabled = false;
            btnSend.IsEnabled = false;
        }

        /// <summary>
        /// Add a message to the listbox
        /// </summary>
        /// <param name="message"></param>
        private void AddMessage(string message)
        {
            Dispatcher.Invoke(() => listChats.Items.Add(message));
        }

        /// <summary>
        /// Listen for button listen click, then initiate a new chatserver, which will listen for clients trying to connect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnListen_Click(object sender, RoutedEventArgs e)
        {
            // If valid fields, start listening for clients, if not, display error message
            if (isValidBufferSize && isValidIP && isValidPort)
            {
                AddMessage("Listening for clients..");
                AddMessage("Type `exit` once a client has connected,");
                AddMessage("or press the stop button at any time to disconnect.");
                chatServer = new ChatServer(GetPort(), GetBufferSize(), (message) => AddMessage(message), () => ToggleServerActiveUI(), () => ToggleButtonsDisconnected());
                chatServer.ListenForClients();
            }
            else
            {
                AddMessage("Faulty settings provided. Please check your input.");
            }
        }

        /// <summary>
        /// Listen for button connect click, then initiate a new chatclient, which will try to connect to the given ip address and port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            // if fields are valid, try to connect, else; display error message
            if (isValidBufferSize && isValidIP && isValidPort)
            {
                // if not username given, use anonymous client
                string Username = !string.IsNullOrEmpty(UsernameTextBox.Text) ? UsernameTextBox.Text : "Anonymous Client";
                chatClient = new ChatClient(new Guid(), Username, null);
                // set dependencies for client, as server also uses clients not all properties should be given in constructor
                chatClient.SetDependencies(IPAddressTextBox.Text, int.Parse(PortTextBox.Text), int.Parse(BufferSizeTextBox.Text),
                    (message) => AddMessage(message), () => ToggleButtonsDisconnected(), () => ToggleButtonsConnected());
                // wait for the connect to happen async
                await Task.Run(() => chatClient.Connect());
            }
            else
            {
                AddMessage("Faulty settings provided. Please check your input.");
            }
        }

        /// <summary>
        /// Listen for the stop button click, on which the server will close all connections
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            chatServer.CloseConnections(true);
            ToggleServerActiveUI();
        }

        /// <summary>
        /// Listen for send Message click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            HandleSendingMessage();
        }

        /// <summary>
        /// Handles sending of the message
        /// </summary>
        private void HandleSendingMessage()
        {
            // if message is not empty
            if (!string.IsNullOrEmpty(txtMessage.Text))
            {
                // decide whether to use client of server side, if both are false, display error message
                if (chatServer != null && chatServer.isServerActive)
                {
                    chatServer.SendMessage(txtMessage.Text);
                }
                else if (chatClient != null && chatClient.isActive)
                {
                    chatClient.SendMessage(txtMessage.Text);
                }
                else
                {
                    AddMessage("Something went wrong. Please try reconnecting.");
                }

                // Clear the textbox for next use
                txtMessage.Clear();
                txtMessage.Focus();
            }
        }

        /// <summary>
        /// Passthrough for toggling buttons with dispatcher
        /// </summary>
        private void ToggleButtonsDisconnected()
        {
            Dispatcher.Invoke(() => ToggleDisconnect());
        }

        /// <summary>
        /// Toggles the buttons and fields in such manner so the state equals that of a disconnected client
        /// </summary>
        private void ToggleDisconnect()
        {
            btnConnect.IsEnabled = true;
            btnListen.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnConnect.IsEnabled = true;

            IPAddressTextBox.IsEnabled = true;
            PortTextBox.IsEnabled = true;
            UsernameTextBox.IsEnabled = true;
        }

        /// <summary>
        /// Passthrough for toggling buttons with dispatcher
        /// </summary>
        private void ToggleButtonsConnected()
        {
            Dispatcher.Invoke(() => ToggleConnect());
        }

        /// <summary>
        /// Toggles the buttons and fields in such manner so the state equals that of a connected client
        /// </summary>
        private void ToggleConnect()
        {
            btnConnect.IsEnabled = false;
            btnListen.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnConnect.IsEnabled = false;

            IPAddressTextBox.IsEnabled = false;
            PortTextBox.IsEnabled = false;
            UsernameTextBox.IsEnabled = false;
        }

        /// <summary>
        /// Button and fields toggler for the server side
        /// </summary>
        private void ToggleServerActiveUI()
        {
            Dispatcher.Invoke(() => btnListen.IsEnabled = !btnListen.IsEnabled);
            Dispatcher.Invoke(() => btnConnect.IsEnabled = !btnConnect.IsEnabled);
            Dispatcher.Invoke(() => btnStop.IsEnabled = !btnStop.IsEnabled);

            Dispatcher.Invoke(() => IPAddressTextBox.IsEnabled = !IPAddressTextBox.IsEnabled);
            Dispatcher.Invoke(() => PortTextBox.IsEnabled = !PortTextBox.IsEnabled);
            Dispatcher.Invoke(() => UsernameTextBox.IsEnabled = !UsernameTextBox.IsEnabled);
        }

        /// <summary>
        /// Check the validity of the input fields
        /// </summary>
        private void CheckValidity()
        {
            // If no server or client is active, don't check and return
            if ((chatClient != null && chatClient.isActive) || (chatServer != null && chatServer.isServerActive)) return;
            // If the buttons exist (weird quark)
            if ((btnListen != null && btnConnect != null))
            {
                // If the fields are valid, set the buttons to enabled, else disable them
                if (isValidBufferSize && isValidIP && isValidPort)
                {
                    btnListen.IsEnabled = true;
                    btnConnect.IsEnabled = true;
                }
                else
                {
                    btnListen.IsEnabled = false;
                    btnConnect.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// if port field is valid, return the value
        /// </summary>
        /// <returns></returns>
        private int GetPort()
        {
            if (isValidPort)
            {
                return int.Parse(PortTextBox.Text);
            }

            return 0;
        }

        /// <summary>
        /// if Buffersize field is valid, return the value
        /// </summary>
        /// <returns></returns>
        private int GetBufferSize()
        {
            if (isValidBufferSize)
            {
                return int.Parse(BufferSizeTextBox.Text);
            }

            return 0;
        }

        /// <summary>
        /// Listen for the IPAddressTextBox change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IPAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Init new Regex to check for matching input
            Regex IP = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            // If no match, field is invalid and set border to red, else; update client IP Address and field is valid, also update validity
            if (!IP.IsMatch(IPAddressTextBox.Text))
            {
                IPAddressTextBox.BorderBrush = Brushes.Red;
                isValidIP = false;
                CheckValidity();
            }
            else
            {
                IPAddressTextBox.BorderBrush = Brushes.Green;
                isValidIP = true;
                CheckValidity();

                if (chatClient != null)
                    chatClient.IP = IPAddressTextBox.Text;
            }
        }

        /// <summary>
        /// Listen for the PortTextBox change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PortTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Init new Regex to check matching input
            Regex Port = new Regex(@"^[0-9]*$");
            // If no match, input is invalid, also update validity
            if (!Port.IsMatch(PortTextBox.Text) || string.IsNullOrEmpty(PortTextBox.Text))
            {
                PortTextBox.BorderBrush = Brushes.Red;
                isValidPort = false;
                CheckValidity();
            }
            else
            {
                PortTextBox.BorderBrush = Brushes.Green;
                isValidPort = true;
                CheckValidity();

                // Update both server and client Port values
                if (chatServer != null)
                    chatServer.PORT = int.Parse(PortTextBox.Text);

                if (chatClient != null)
                    chatClient.PORT = int.Parse(PortTextBox.Text);
            }
        }

        /// <summary>
        /// Listen for the BufferSizeTextBox change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BufferSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Init new Regex to check matching input for buffersize
            Regex BufferSize = new Regex(@"^[0-9]*$");
            // buffersize can be between 0 and 1024, and only contains numbers -> if valid, field is valid
            if (!BufferSize.IsMatch(BufferSizeTextBox.Text)
                || string.IsNullOrEmpty(BufferSizeTextBox.Text)
                || int.Parse(BufferSizeTextBox.Text) > 1024
                || int.Parse(BufferSizeTextBox.Text) < 0)
            {
                BufferSizeTextBox.BorderBrush = Brushes.Red;
                isValidBufferSize = false;
                CheckValidity();
            }
            else
            {
                BufferSizeTextBox.BorderBrush = Brushes.Green;
                isValidBufferSize = true;
                CheckValidity();

                // Update both server and client buffer size values
                if (chatServer != null)
                    chatServer.BUFFER_SIZE = int.Parse(BufferSizeTextBox.Text);

                if (chatClient != null)
                    chatClient.BUFFER_SIZE = int.Parse(BufferSizeTextBox.Text);
            }
        }

        /// <summary>
        /// Listen for keypressed on textmessage box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtMessageKeypressed(object sender, KeyEventArgs e)
        {
            // If key pressed is not enter, we are not interested
            if (!(e.Key == Key.Enter)) return;

            // if a client or server instance exists, send message.
            if ((chatServer != null && chatServer.isServerActive && chatServer.clients.Count > 0) || (chatClient != null && chatClient.isActive))
            {
                e.Handled = true;
                HandleSendingMessage();
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// Listen for input on the textmessage box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            // If an instance of server or client exists and they are active or the server contains more than 1 client, user can send messages.
            if ((chatServer != null && chatServer.isServerActive && chatServer.clients.Count > 0) || (chatClient != null && chatClient.isActive))
            {
                btnSend.IsEnabled = true;
            }
            else
            {
                btnSend.IsEnabled = false;
            }
        }
    }
}
