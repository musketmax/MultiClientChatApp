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
    /// </summary>
    public partial class MainWindow : Window
    {
        private ChatServer chatServer;
        private ChatClient chatClient;

        private bool isValidPort = false;
        private bool isValidBufferSize = false;
        private bool isValidIP = false;

        public MainWindow()
        {
            InitializeComponent();
            CheckValidity();

            btnStop.IsEnabled = false;
            btnSend.IsEnabled = false;
        }

        private void AddMessage(string message)
        {
            Dispatcher.Invoke(() => listChats.Items.Add(message));
        }

        private void BtnListen_Click(object sender, RoutedEventArgs e)
        {
            if (isValidBufferSize && isValidIP && isValidPort)
            {
                AddMessage("Listening for clients..");
                chatServer = new ChatServer(GetPort(), GetBufferSize(), (message) => AddMessage(message), () => ToggleServerActiveUI());
                chatServer.ListenForClients();
            }
            else
            {
                AddMessage("Faulty settings provided. Please check your input.");
            }
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (isValidBufferSize && isValidIP && isValidPort)
            {
                string Username = !string.IsNullOrEmpty(UsernameTextBox.Text) ? UsernameTextBox.Text : "Anonymous Client";
                chatClient = new ChatClient(new Guid(), Username, null);
                chatClient.SetDependencies(IPAddressTextBox.Text, int.Parse(PortTextBox.Text), int.Parse(BufferSizeTextBox.Text),
                    (message) => AddMessage(message), () => ToggleButtonsDisconnected(), () => ToggleButtonsConnected());
                await Task.Run(() => chatClient.Connect());
            }
            else
            {
                AddMessage("Faulty settings provided. Please check your input.");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            chatServer.CloseConnections();
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            HandleSendingMessage();
        }

        private void HandleSendingMessage()
        {
            if (!string.IsNullOrEmpty(txtMessage.Text))
            {
                if (chatServer != null)
                {
                    chatServer.SendMessage(txtMessage.Text);
                }
                else if (chatClient != null)
                {
                    chatClient.SendMessage(txtMessage.Text);
                }
                else
                {
                    AddMessage("Something went wrong. Please try reconnecting.");
                }

                txtMessage.Clear();
                txtMessage.Focus();
            }
        }

        private void ToggleButtonsDisconnected()
        {
            Dispatcher.Invoke(() => ToggleDisconnect());
        }

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

        private void ToggleButtonsConnected()
        {
            Dispatcher.Invoke(() => ToggleConnect());
        }

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

        private void ToggleServerActiveUI()
        {
            Dispatcher.Invoke(() => btnListen.IsEnabled = !btnListen.IsEnabled);
            Dispatcher.Invoke(() => btnConnect.IsEnabled = !btnConnect.IsEnabled);
            Dispatcher.Invoke(() => btnStop.IsEnabled = !btnStop.IsEnabled);

            Dispatcher.Invoke(() => IPAddressTextBox.IsEnabled = !IPAddressTextBox.IsEnabled);
            Dispatcher.Invoke(() => PortTextBox.IsEnabled = !PortTextBox.IsEnabled);
            Dispatcher.Invoke(() => UsernameTextBox.IsEnabled = !UsernameTextBox.IsEnabled);
        }

        private void CheckValidity()
        {
            if ((chatClient != null && chatClient.isActive) || (chatServer != null && chatServer.isServerActive)) return;
            if ((btnListen != null && btnConnect != null))
            {
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

        private int GetPort()
        {
            if (isValidPort)
            {
                return int.Parse(PortTextBox.Text);
            }

            return 0;
        }

        private int GetBufferSize()
        {
            if (isValidBufferSize)
            {
                return int.Parse(BufferSizeTextBox.Text);
            }

            return 0;
        }

        private string GetIPAddress()
        {
            if (isValidIP)
            {
                return IPAddressTextBox.Text;
            }

            return "";
        }

        private void IPAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Regex IP = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
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

        private void PortTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Regex Port = new Regex(@"^[0-9]*$");
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

                if (chatServer != null)
                    chatServer.PORT = int.Parse(PortTextBox.Text);

                if (chatClient != null)
                    chatClient.PORT = int.Parse(PortTextBox.Text);
            }
        }

        private void BufferSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Regex BufferSize = new Regex(@"^[0-9]*$");
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

                if (chatServer != null)
                    chatServer.BUFFER_SIZE = int.Parse(BufferSizeTextBox.Text);
                AddMessage("changed server!");

                if (chatClient != null)
                    chatClient.BUFFER_SIZE = int.Parse(BufferSizeTextBox.Text);
                AddMessage("changed client!");
            }
        }

        private void txtMessageKeypressed(object sender, KeyEventArgs e)
        {
            if (!(e.Key == Key.Enter)) return;

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

        private void TxtMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
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
