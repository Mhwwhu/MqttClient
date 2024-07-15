using System.Collections.Concurrent;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Internal;
using MQTTnet.Protocol;

namespace Client
{
	public class MqttClient
	{
		private ConcurrentQueue<MqttApplicationMessage> _recvQueue;
		private ConcurrentQueue<MqttApplicationMessage> _pubQueue;
		public string Host { get; }
		public int Port { get; }
		public string ID { get; }
		public MqttClientConnectResultCode? ResultCode { get; private set; } = null;
		public MqttClient(string host, int port, string? id = null)
		{
			Host = host;
			Port = port;
			_recvQueue = new ConcurrentQueue<MqttApplicationMessage>();
			_pubQueue = new ConcurrentQueue<MqttApplicationMessage>();
			ID = id ?? Guid.NewGuid().ToString();
		}
		public void StartListen(string topic, CancellationToken cancel)
		{
			var factory = new MqttFactory();
			var client = factory.CreateMqttClient();
			var options = new MqttClientOptionsBuilder()
				.WithTcpServer(Host, Port)
				.WithClientId(ID)
				.WithCleanSession()
				.Build();
			var connResult = client.ConnectAsync(options).GetAwaiter().GetResult();
			if (connResult.ResultCode != MqttClientConnectResultCode.Success)
			{
				ResultCode = connResult.ResultCode;
				return;
			}
			client.SubscribeAsync(topic).GetAwaiter();
			client.ApplicationMessageReceivedAsync += e =>
			{
				_recvQueue.Enqueue(e.ApplicationMessage);
				return Task.CompletedTask;
			};
			while (!cancel.IsCancellationRequested) ;
		}
		public void StartPublish(CancellationToken cancel)
		{
			var factory = new MqttFactory();
			var client = factory.CreateMqttClient();
			var options = new MqttClientOptionsBuilder()
				.WithTcpServer(Host, Port)
				.WithClientId(ID)
				.WithCleanSession()
				.Build();
			var connResult = client.ConnectAsync(options).GetAwaiter().GetResult();
			if (connResult.ResultCode != MqttClientConnectResultCode.Success)
			{
				ResultCode = connResult.ResultCode;
				return;
			}
			while (!cancel.IsCancellationRequested)
			{
				if (_pubQueue.TryDequeue(out var message))
				{
					client.PublishAsync(message).GetAwaiter();
				}
			}
		}
		public void PublishMessage(MqttApplicationMessage message)
		{
			_pubQueue.Enqueue(message);
		}
		public MqttApplicationMessage? GetRecvMessage()
		{
			_recvQueue.TryDequeue(out var message);
			return message;
		}
	}
}
