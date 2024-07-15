using CommandLine;
using System.ComponentModel;

namespace CmdLine
{
	public class Options
	{
		[Option('h', "host", Required = false, HelpText = "Host to a mqtt broker")]
		public string? Host {  get; set; }
		[Option('p', "port", Required = false, HelpText = "Port of the host")]
		public int? Port { get; set; }
		[Option('d', "display", Required = false, HelpText = "Display format: text | hex | bin")]
		[TypeConverter(typeof(OutputFormConverter))]
		public OutputForm? DisplayForm { get; set; }
		[Option("pub", Required = false, HelpText = "Publish message to a certain topic", Default = false)]
		public bool Publish { get; set; }
		[Option('t', Required = true, HelpText = "Select a topic")]
		public string Topic { get; set; }

		public void RunOptionsAndReturnExitCode()
		{
		}
		public static void HandleParseError(IEnumerable<Error> err)
		{
			Console.WriteLine("Usage: [-h <host>] [-p <port>] [-d <display form>]");
		}
	}
}
