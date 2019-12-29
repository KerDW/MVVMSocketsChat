using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MVVMSocketConsumer.ViewModel
{
    class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        CancellationTokenSource cts = new CancellationTokenSource();
        ClientWebSocket socket = new ClientWebSocket();


        public ViewModel()
        {
            join = new RelayCommand<string>(JoinChat);
            send = new RelayCommand<string>(SendMessage);
        }

        public void JoinChat(string btName)
        {
            Start();
        }

        public RelayCommand<string> join { get; set; }
        public RelayCommand<string> send { get; set; }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private String _name;
        public String name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }

        private String _message;
        public String message
        {
            get { return _message; }
            set
            {
                _message = value;
                NotifyPropertyChanged();
            }
        }
        private ObservableCollection<String> _messages = new ObservableCollection<String>();

        public ObservableCollection<String> messages
        {
            get { return _messages; }
            set
            {
                _messages = value;
                NotifyPropertyChanged();
            }
        }

        public async void SendMessage(string btName)
        {
            if (message == "bye")
            {
                cts.Cancel();
                return;
            }
            byte[] sendBytes = Encoding.UTF8.GetBytes(message);
            var sendBuffer = new ArraySegment<byte>(sendBytes);
            await socket.SendAsync(sendBuffer, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: cts.Token);
        }

        public async Task Start()
        {
            string nom = name;
            string wsUri = string.Format("wss://localhost:44332/api/websocket?nom={0}", nom);
            await socket.ConnectAsync(new Uri(wsUri), cts.Token);

            await Task.Factory.StartNew(
                async () =>
                {
                    var rcvBytes = new byte[128];
                    var rcvBuffer = new ArraySegment<byte>(rcvBytes);
                    while (true)
                    {
                        WebSocketReceiveResult rcvResult = await socket.ReceiveAsync(rcvBuffer, cts.Token);
                        byte[] msgBytes = rcvBuffer.Skip(rcvBuffer.Offset).Take(rcvResult.Count).ToArray();
                        string rcvMsg = Encoding.UTF8.GetString(msgBytes);
                        Console.WriteLine("{0}", rcvMsg);
                        Application.Current.Dispatcher.Invoke(new Action(() => {
                            messages.Add(rcvMsg);
                        }));
                        Console.WriteLine(messages.Count);
                    }
                }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}
