using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aspnetclient
{
    public class ClientConnection
    {
        private static readonly TimeSpan INTERVAL = TimeSpan.FromMilliseconds(1000);
        private List<HubConnection> _hubConnections;
        private List<IHubProxy> _hubProxy = new List<IHubProxy>();
        private List<IClientTransport> _transportList = new List<IClientTransport>();
        private Counter _counter;
        private Timer _timer;
        private bool _start = false;
        private bool _disposed = false;
        private string _content;
        private string _serverMethod;
        private string _clientMethod;
        private string _transport;

        public ClientConnection(ArgsOption args, Counter counter)
        {
            _counter = counter;
            _serverMethod = args.ServerMethod;
            _clientMethod = args.ClientMethod;
            _transport = args.Transport;
            InitSendingContent(args.SendSize);
            CreateConnections(args);
            PrepareTimer();
        }

        private void InitSendingContent(int sz)
        {
            var rnd = new Random();
            byte[] content = new byte[sz];
            rnd.NextBytes(content);
            _content = Encoding.UTF8.GetString(content);
        }

        private void PrepareTimer()
        {
            _timer = new Timer(PeriodicSend, this, INTERVAL, INTERVAL);
        }

        private void CreateConnections(ArgsOption args)
        {
            var count = args.Clients;
            var appUrl = args.AppUrl;
            var connections = (from i in Enumerable.Range(0, count)
                               select new HubConnection(appUrl)).ToList();
            foreach (var c in connections)
            {
                var hubProxy = c.CreateHubProxy(args.HubName);
                hubProxy.On<string, string>(args.ClientMethod, (name, timestamp) => {
                    _counter.Latency(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - Convert.ToInt64(timestamp));
                });
                hubProxy.On<string, string>("broadcastMessage", (name, message) => Console.WriteLine($"Received message from broadcast {name} {message}"));
                _hubProxy.Add(hubProxy);
                c.Closed += () =>
                {
                    Console.WriteLine("Connection closed");
                };
                c.Error += e =>
                {
                    Console.WriteLine(e.Message);
                };
                if (args.EnableTrace == 1)
                {
                    c.TraceLevel = TraceLevels.All;
                    c.TraceWriter = Console.Out;
                }
            }
            _hubConnections = connections;
        }

        public void StartSend()
        {
            _start = true;
        }

        public async Task StartConnect()
        {
            var tasks = new List<Task>();
            foreach (var c in _hubConnections)
            {
                tasks.Add(c.Start(createClientTransportAndCached(_transport)));
            }
            await Task.WhenAll(tasks);
        }

        public async Task BatchStartConnect(int concurrentConnection)
        {
            var swCollect = new Stopwatch();
            swCollect.Start();
            await Utils.BatchProcess(_hubConnections, Utils.StartConnection, concurrentConnection);
            swCollect.Stop();
            Console.WriteLine($"Total time spent on connection: {swCollect.Elapsed.TotalMilliseconds} ms");
        }

        public Task Broadcast(string msg)
        {
            return _hubProxy[0].Invoke("Send", "client-broadcast", msg);
        }

        public void PeriodicSend(object state)
        {
            var cc = (ClientConnection)state;
            cc.PeriodicSendImpl();
        }

        public void PeriodicSendImpl()
        {
            if (_start)
            {
                Task.Run(async () =>
                {
                    await SendMessage();
                });
            }
        }

        private IClientTransport createClientTransport(string transport)
        {
            switch (transport)
            {
                case "WebSocketTransport":
                    return new WebSocketTransport();
                case "LongPollingTransport":
                    return new LongPollingTransport();
                case "ServerSentEventsTransport":
                    return new ServerSentEventsTransport();
                default:
                    throw new NotSupportedException($"wrong transport type {transport}");
            }
        }

        private IClientTransport createClientTransportAndCached(string transport)
        {
            var clientTransport = createClientTransport(transport);
            _transportList.Add(clientTransport);
            return clientTransport;
        }

        private Task SendMessage()
        {
            var tasks = new List<Task>();
            foreach (var hubProxy in _hubProxy)
            {
                tasks.Add(hubProxy.Invoke(_serverMethod, _content, $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"));
            }
            return Task.WhenAll(tasks);
        }

        public void Stop()
        {
            foreach (var c in _hubConnections)
            {
                c.Stop();
                c.Dispose();
            }
        }

        public void Dispose()
        {
            Console.WriteLine("Server stopping...");
            Stop();
            Task.Delay(1000).Wait(); // wait for draining out all sending message
            if (!_disposed)
            {
                _timer.Dispose();
                foreach (var t in _transportList)
                {
                    t.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
