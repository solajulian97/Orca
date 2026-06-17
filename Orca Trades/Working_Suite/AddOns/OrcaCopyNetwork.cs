#region Using declarations
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
#endregion

namespace NinjaTrader.NinjaScript.AddOns
{
	public enum OrcaCopyNetworkMode
	{
		Off,
		LeaderServer,
		RemoteFollower
	}

	public sealed class OrcaCopyNetworkMessage
	{
		public string MessageType { get; set; }
		public string LeaderAccountName { get; set; }
		public string LeaderOrderKey { get; set; }
		public string InstrumentFullName { get; set; }
		public string OrderAction { get; set; }
		public string OrderType { get; set; }
		public string OrderState { get; set; }
		public string TimeInForce { get; set; }
		public string Oco { get; set; }
		public string Name { get; set; }
		public int Quantity { get; set; }
		public int Filled { get; set; }
		public double LimitPrice { get; set; }
		public double StopPrice { get; set; }
		public double AverageFillPrice { get; set; }
		public double ExecutionPrice { get; set; }
		public int ExecutionQuantity { get; set; }
		public bool IsProtective { get; set; }
		public bool IsCancel { get; set; }
		public bool IsChange { get; set; }
		public long TimestampUtcTicks { get; set; }
	}

	public sealed class OrcaCopyNetworkEventArgs : EventArgs
	{
		public OrcaCopyNetworkEventArgs(OrcaCopyNetworkMessage message)
		{
			Message = message;
		}

		public OrcaCopyNetworkMessage Message { get; private set; }
	}

	public sealed class OrcaCopyNetworkStatusEventArgs : EventArgs
	{
		public OrcaCopyNetworkStatusEventArgs(string status)
		{
			Status = status;
		}

		public string Status { get; private set; }
	}

	public sealed class OrcaCopyNetwork : IDisposable
	{
		private readonly ConcurrentDictionary<Guid, TcpClient> serverClients = new ConcurrentDictionary<Guid, TcpClient>();
		private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
		private readonly object serializerLock = new object();
		private readonly object clientWriteLock = new object();
		private CancellationTokenSource cancelSource;
		private TcpClient outboundClient;
		private TcpListener listener;
		private volatile bool running;

		public event EventHandler<OrcaCopyNetworkEventArgs> MessageReceived;
		public event EventHandler<OrcaCopyNetworkStatusEventArgs> StatusChanged;

		public bool IsRunning
		{
			get { return running; }
		}

		public void StartServer(int port)
		{
			Stop();
			cancelSource = new CancellationTokenSource();
			listener = new TcpListener(IPAddress.Any, Math.Max(1, Math.Min(65535, port)));
			listener.Start();
			running = true;
			RaiseStatus("Leader server listening on port " + port);
			Task.Factory.StartNew(() => AcceptLoop(cancelSource.Token), cancelSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}

		public void ConnectClient(string host, int port)
		{
			Stop();
			cancelSource = new CancellationTokenSource();
			outboundClient = new TcpClient();
			outboundClient.NoDelay = true;
			outboundClient.Connect(string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim(), Math.Max(1, Math.Min(65535, port)));
			running = true;
			RaiseStatus("Remote follower connected to " + host + ":" + port);
			Task.Factory.StartNew(() => ReadLoop(outboundClient, cancelSource.Token, "leader"), cancelSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}

		public void Broadcast(OrcaCopyNetworkMessage message)
		{
			if (message == null || !running)
				return;

			string payload = Serialize(message) + "\n";
			byte[] bytes = Encoding.UTF8.GetBytes(payload);

			if (outboundClient != null && outboundClient.Connected)
				WriteClient(outboundClient, bytes, clientWriteLock);

			foreach (var pair in serverClients.ToArray()) {
				TcpClient client = pair.Value;
				if (client == null || !client.Connected) {
					RemoveServerClient(pair.Key, client);
					continue;
				}

				try { WriteClient(client, bytes, client); }
				catch {
					RemoveServerClient(pair.Key, client);
				}
			}
		}

		public void Stop()
		{
			running = false;
			try {
				if (cancelSource != null)
					cancelSource.Cancel();
			} catch { }

			try {
				if (listener != null)
					listener.Stop();
			} catch { }
			listener = null;

			try {
				if (outboundClient != null)
					outboundClient.Close();
			} catch { }
			outboundClient = null;

			foreach (var pair in serverClients.ToArray())
				RemoveServerClient(pair.Key, pair.Value);

			try {
				if (cancelSource != null)
					cancelSource.Dispose();
			} catch { }
			cancelSource = null;
			RaiseStatus("Network stopped");
		}

		public void Dispose()
		{
			Stop();
		}

		private void AcceptLoop(CancellationToken token)
		{
			while (!token.IsCancellationRequested && listener != null) {
				try {
					TcpClient client = listener.AcceptTcpClient();
					client.NoDelay = true;
					Guid id = Guid.NewGuid();
					serverClients[id] = client;
					RaiseStatus("Remote follower connected: " + SafeEndpoint(client));
					Task.Factory.StartNew(() => ReadLoop(client, token, id.ToString("N")), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
				} catch (ObjectDisposedException) {
					return;
				} catch (SocketException) {
					if (!token.IsCancellationRequested)
						Thread.Sleep(100);
				} catch (Exception ex) {
					RaiseStatus("Network accept error: " + ex.Message);
					Thread.Sleep(250);
				}
			}
		}

		private void ReadLoop(TcpClient client, CancellationToken token, string source)
		{
			try {
				using (NetworkStream stream = client.GetStream())
				using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) {
					while (!token.IsCancellationRequested && client.Connected) {
						string line = reader.ReadLine();
						if (line == null)
							break;
						if (line.Length == 0)
							continue;

						OrcaCopyNetworkMessage message = Deserialize(line);
						if (message != null)
							RaiseMessage(message);
					}
				}
			} catch (IOException) {
			} catch (ObjectDisposedException) {
			} catch (Exception ex) {
				RaiseStatus("Network read error from " + source + ": " + ex.Message);
			} finally {
				if (!string.Equals(source, "leader", StringComparison.OrdinalIgnoreCase)) {
					Guid id;
					if (Guid.TryParse(source, out id))
						RemoveServerClient(id, client);
				}
			}
		}

		private string Serialize(OrcaCopyNetworkMessage message)
		{
			lock (serializerLock)
				return serializer.Serialize(message);
		}

		private OrcaCopyNetworkMessage Deserialize(string line)
		{
			try {
				lock (serializerLock)
					return serializer.Deserialize<OrcaCopyNetworkMessage>(line);
			} catch (Exception ex) {
				RaiseStatus("Network JSON parse error: " + ex.Message);
				return null;
			}
		}

		private static void WriteClient(TcpClient client, byte[] bytes, object sync)
		{
			lock (sync) {
				NetworkStream stream = client.GetStream();
				stream.Write(bytes, 0, bytes.Length);
				stream.Flush();
			}
		}

		private void RemoveServerClient(Guid id, TcpClient client)
		{
			TcpClient removed;
			serverClients.TryRemove(id, out removed);
			try {
				if (client != null)
					client.Close();
			} catch { }
		}

		private static string SafeEndpoint(TcpClient client)
		{
			try { return client.Client.RemoteEndPoint.ToString(); }
			catch { return "unknown"; }
		}

		private void RaiseMessage(OrcaCopyNetworkMessage message)
		{
			EventHandler<OrcaCopyNetworkEventArgs> handler = MessageReceived;
			if (handler != null)
				handler(this, new OrcaCopyNetworkEventArgs(message));
		}

		private void RaiseStatus(string status)
		{
			EventHandler<OrcaCopyNetworkStatusEventArgs> handler = StatusChanged;
			if (handler != null)
				handler(this, new OrcaCopyNetworkStatusEventArgs(status));
		}
	}
}
