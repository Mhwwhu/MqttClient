using MQTTnet;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using System.Text;
using CommandLine;
using Microsoft.Extensions.Configuration;
using CmdLine;
using Client;
using System.Collections;

namespace Program
{
	public class Program
	{
		static private bool _escPressed = false;
		static private string _host;
		static private int _port;
		static private string _outputFile;
		static private OutputForm _outputForm;
		static private bool _outputToFile;
		static private bool _append;
		static void Main(string[] args)
		{
			BuildConfiguration();
			ParserResult<Options> result;
			try
			{
				result = Parser.Default.ParseArguments<Options>(args)
					.WithParsed<Options>(opts => opts.RunOptionsAndReturnExitCode())
					.WithNotParsed(err => Options.HandleParseError(err));
				var opt = result.Value;
				if (opt == null) return;
				var host = opt.Host ?? _host;
				var port = opt.Port ?? _port;
				var displayForm = opt.DisplayForm ?? _outputForm;
				var isPublisher = opt.Publish;
				var topic = opt.Topic;
				var client = new MqttClient(host, port, "test");
				if (isPublisher)
				{
					RunPublish(client, topic);
				}
				else
				{
					RunSubscriber(client, topic);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return;
			}
		}
		static void RunPublish(MqttClient client, string topic)
		{
			var cts = new CancellationTokenSource();
			Task.Run(() => client.StartPublish(cts.Token));
			while (!_escPressed)
			{
				var msgstr = Console.ReadLine();
				var message = new MqttApplicationMessageBuilder()
					.WithTopic(topic)
					.WithPayload (msgstr)
					.WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
					.Build();
				client.PublishMessage(message);
			}
		}
		static void RunSubscriber(MqttClient client, string topic)
		{
			Console.WriteLine("Press ESC to exit");
			Task.Run(() => WatchForKeyPress());
			bool isBinary = _outputForm == OutputForm.BIN;
			var cts = new CancellationTokenSource();
			Task.Run(() => client.StartListen(topic, cts.Token));
			StreamWriter? textfile = null;
			FileStream? binfile = null;
			if (!isBinary)
			{
				if (_outputFile == string.Empty) _outputFile = "temp.txt";
				textfile = new StreamWriter(_outputFile, _append);
			}
			else
			{
				if (_outputFile == string.Empty) _outputFile = "temp.bin";
				if (_append) binfile = new FileStream(_outputFile, FileMode.Append);
				else binfile = new FileStream(_outputFile, FileMode.OpenOrCreate);
			}
			while (!_escPressed)
			{
				var message = client.GetRecvMessage();
				if (message == null) continue;
				ArraySegment<byte> payload = message.PayloadSegment;
				string text = "";
				switch (_outputForm)
				{
					case OutputForm.TEXT:
						text = Encoding.UTF8.GetString(payload);
						break;
					case OutputForm.HEX:
						text = BitConverter.ToString(payload.ToArray()).Replace("-", " ");
						break;
					default:
						isBinary = true;
						break;
				}
				if(_outputToFile)
				{
					if (!isBinary)
					{
						textfile!.WriteLine(text);
					}
					else
					{
						binfile!.Write(payload);
					}
				}
				else
				{
					Console.WriteLine(text);
				}
			}
			if (!isBinary) textfile!.Close();
			else binfile!.Close();
		}
		static void BuildConfiguration()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("config.json", false)
				.Build();
			_host = configuration["host"]!;
			_port = int.Parse(configuration["port"]!);
			_outputFile = configuration["output_file"]!;
			_outputToFile = bool.Parse(configuration["output_to_file"]!);
			_append = bool.Parse(configuration["append"]!);
			var outputForm = configuration["display_form"]!;
			switch (outputForm.ToUpper())
			{
				case "TEXT":
					_outputForm = OutputForm.TEXT;
					break;
				case "HEX":
					_outputForm = OutputForm.HEX;
					break;
				case "BIN":
					_outputForm = OutputForm.BIN;
					break;
				default:
					Console.WriteLine("Unrecognized output form: " + outputForm);
					Console.WriteLine("Options: text, hex, bin");
					break;
			}
		}
		static void WatchForKeyPress()
		{
			while (true)
			{
				var key = Console.ReadKey(true);

				if (key.Key == ConsoleKey.Escape)
				{
					_escPressed = true;
					break;
				}
			}
		}
	}

}
