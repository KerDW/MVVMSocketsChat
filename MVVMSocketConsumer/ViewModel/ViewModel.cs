using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MVVMSocketConsumer.ViewModel
{
    class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModel()
        {

            Start().Wait();

        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public async Task Start()
        {
            Console.Write("Nom: ");
            string nom = Console.ReadLine();

            Console.Write("Connectant....");
            var cts = new CancellationTokenSource();
            var socket = new ClientWebSocket();
            string wsUri = string.Format("wss://localhost:44332/api/websocket?nom={0}", nom);
            await socket.ConnectAsync(new Uri(wsUri), cts.Token);
            Console.WriteLine(socket.State);

            Task.Factory.StartNew(
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
                    }
                }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            while (true)
            {
                var missatge = Console.ReadLine();
                if (missatge == "Adeu")
                {
                    cts.Cancel();
                    return;
                }
                byte[] sendBytes = Encoding.UTF8.GetBytes(missatge);
                var sendBuffer = new ArraySegment<byte>(sendBytes);
                await socket.SendAsync(sendBuffer, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: cts.Token);
            }
        }
    }
}
