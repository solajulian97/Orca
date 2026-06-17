using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using NinjaTrader.NinjaScript;

namespace OrcaTradeCopier
{
    /// <summary>
    /// Represents the serialized payload representing an order execution or update
    /// broadcasted from the Leader to the Network Followers.
    /// </summary>
    public class TradeMessage
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string InstrumentName { get; set; }
        public string OrderAction { get; set; } // BUY, SELL
        public string OrderType { get; set; }   // MARKET, LIMIT, STOP, STOP_LIMIT
        public string ActionType { get; set; }  // SUBMIT, MODIFY, CANCEL
        public string LeaderOrderId { get; set; }
        public int Quantity { get; set; }
        public double LimitPrice { get; set; }
        public double StopPrice { get; set; }
        public string AtmTemplateName { get; set; }
        public long TimestampTicks { get; set; } = DateTime.UtcNow.Ticks;
    }

    /// <summary>
    /// Lightweight TCP Client/Server class to facilitate sub-millisecond
    /// trade broadcasting over the Local Area Network (LAN).
    /// </summary>
    public class OrcaTradeCopierNetwork : IDisposable
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly ConcurrentBag<TcpClient> _connectedClients = new ConcurrentBag<TcpClient>();
        private TcpClient _client;
        private NetworkStream _clientStream;
        private bool _isServer;
        private bool _isRunning;

        // Events
        public event Action<TradeMessage> OnTradeMessageReceived;
        public event Action<string> OnLogMessage;
        public event Action<bool> OnConnectionStatusChanged;

        public bool IsRunning => _isRunning;

        /// <summary>
        /// Starts the TCP socket server to broadcast orders to follower machines.
        /// </summary>
        public void StartServer(int port)
        {
            if (_isRunning) Stop();

            _isServer = true;
            _isRunning = true;
            _cts = new CancellationTokenSource();

            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                Log($"[Server] Started on port {port}. Waiting for connections...");
                OnConnectionStatusChanged?.Invoke(true);

                Task.Run(() => AcceptClientsAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                Log($"[Server Error] Failed to start: {ex.Message}");
                Stop();
            }
        }

        /// <summary>
        /// Connects to a leader machine to receive broadcasted trades.
        /// </summary>
        public void StartClient(string ipAddress, int port)
        {
            if (_isRunning) Stop();

            _isServer = false;
            _isRunning = true;
            _cts = new CancellationTokenSource();

            Task.Run(() => ConnectAndListenAsync(ipAddress, port, _cts.Token));
        }

        /// <summary>
        /// Shuts down all active network operations cleanly.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cts?.Cancel();

            try
            {
                _listener?.Stop();
                _listener = null;
            }
            catch (Exception ex) { Log($"[Network Shutdown] Listener error: {ex.Message}"); }

            // Close all connected server clients
            while (_connectedClients.TryTake(out var client))
            {
                try { client.Close(); } catch { }
            }

            // Close client connection
            try
            {
                _clientStream?.Close();
                _client?.Close();
                _client = null;
            }
            catch { }

            Log("[Network] Disconnected and shut down.");
            OnConnectionStatusChanged?.Invoke(false);
        }

        /// <summary>
        /// Broadcasts a trade message from the server to all connected client machines.
        /// </summary>
        public void BroadcastTrade(TradeMessage msg)
        {
            if (!_isServer || !_isRunning) return;

            string json = SerializeMessage(msg) + "\n"; // Newline delimiter
            byte[] data = Encoding.UTF8.GetBytes(json);

            var activeClients = new ConcurrentBag<TcpClient>();

            while (_connectedClients.TryTake(out var client))
            {
                if (client.Connected)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            var stream = client.GetStream();
                            lock (stream)
                            {
                                stream.Write(data, 0, data.Length);
                                stream.Flush();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"[Server] Failed to send to client: {ex.Message}");
                            try { client.Close(); } catch { }
                            return; // Do not add back
                        }
                    });

                    activeClients.Add(client);
                }
            }

            // Put active clients back in the bag
            foreach (var client in activeClients)
            {
                _connectedClients.Add(client);
            }
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _connectedClients.Add(client);
                    Log($"[Server] Client connected from: {client.Client.RemoteEndPoint}");
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Log($"[Server Error] Accepting client failed: {ex.Message}");
                    }
                    break;
                }
            }
        }

        private async Task ConnectAndListenAsync(string ipAddress, int port, CancellationToken token)
        {
            while (!token.IsCancellationRequested && _isRunning)
            {
                try
                {
                    Log($"[Client] Connecting to {ipAddress}:{port}...");
                    _client = new TcpClient();

                    var connectTask = _client.ConnectAsync(ipAddress, port);
                    var delayTask = Task.Delay(5000, token); // 5s connection timeout

                    var completedTask = await Task.WhenAny(connectTask, delayTask);
                    if (completedTask == delayTask || !_client.Connected)
                    {
                        Log("[Client Warning] Connection timed out. Retrying in 5 seconds...");
                        _client?.Close();
                        await Task.Delay(5000, token);
                        continue;
                    }

                    _clientStream = _client.GetStream();
                    Log("[Client] Connected to leader successfully.");
                    OnConnectionStatusChanged?.Invoke(true);

                    using (var reader = new StreamReader(_clientStream, Encoding.UTF8))
                    {
                        while (!token.IsCancellationRequested && _client.Connected)
                        {
                            string line = await reader.ReadLineAsync();
                            if (line == null) break; // Server disconnected

                            var msg = DeserializeMessage(line);
                            if (msg != null)
                            {
                                Task.Run(() => OnTradeMessageReceived?.Invoke(msg), token);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Log($"[Client Connection Lost] Error: {ex.Message}. Reconnecting in 5 seconds...");
                    }
                }
                finally
                {
                    OnConnectionStatusChanged?.Invoke(false);
                    _clientStream?.Close();
                    _client?.Close();
                    _client = null;
                }

                if (!token.IsCancellationRequested)
                {
                    await Task.Delay(5000, token);
                }
            }
        }

        private string SerializeMessage(TradeMessage msg)
        {
            // Simple robust JSON writer to avoid dependency assembly loading failures in NT8
            return $"{{" +
                   $"\"MessageId\":\"{msg.MessageId}\"," +
                   $"\"InstrumentName\":\"{msg.InstrumentName}\"," +
                   $"\"OrderAction\":\"{msg.OrderAction}\"," +
                   $"\"OrderType\":\"{msg.OrderType}\"," +
                   $"\"ActionType\":\"{msg.ActionType}\"," +
                   $"\"LeaderOrderId\":\"{msg.LeaderOrderId}\"," +
                   $"\"Quantity\":{msg.Quantity}," +
                   $"\"LimitPrice\":{msg.LimitPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                   $"\"StopPrice\":{msg.StopPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                   $"\"AtmTemplateName\":\"{msg.AtmTemplateName}\"," +
                   $"\"TimestampTicks\":{msg.TimestampTicks}" +
                   $"}}";
        }

        private TradeMessage DeserializeMessage(string json)
        {
            try
            {
                // Robust dependency-free manual JSON parsing to maximize NT8 reliability
                var msg = new TradeMessage();
                json = json.Trim('{', '}', '\r', '\n', ' ');
                string[] pairs = json.Split(new[] { "\",\"", "," }, StringSplitOptions.None);

                foreach (var pair in pairs)
                {
                    string[] parts = pair.Split(new[] { "\":", ":" }, StringSplitOptions.None);
                    if (parts.Length < 2) continue;

                    string key = parts[0].Trim('"', ' ');
                    string val = parts[1].Trim('"', ' ');

                    switch (key)
                    {
                        case "MessageId": msg.MessageId = val; break;
                        case "InstrumentName": msg.InstrumentName = val; break;
                        case "OrderAction": msg.OrderAction = val; break;
                        case "OrderType": msg.OrderType = val; break;
                        case "ActionType": msg.ActionType = val; break;
                        case "LeaderOrderId": msg.LeaderOrderId = val; break;
                        case "Quantity": msg.Quantity = int.Parse(val); break;
                        case "LimitPrice": msg.LimitPrice = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "StopPrice": msg.StopPrice = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "AtmTemplateName": msg.AtmTemplateName = val; break;
                        case "TimestampTicks": msg.TimestampTicks = long.Parse(val); break;
                    }
                }
                return msg;
            }
            catch (Exception ex)
            {
                Log($"[Serialization Error] Failed to parse message: {ex.Message}");
                return null;
            }
        }

        private void Log(string message)
        {
            OnLogMessage?.Invoke(message);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
