using CommandLine;

namespace aspnetclient
{
    public class ArgsOption
    {
        [Option("AppUrl", Required = true, HelpText = "Application URL to connect")]
        public string AppUrl { get; set; }

        [Option("Clients", Required = false, Default = 10, HelpText = "Specify the connection client number")]
        public int Clients { get; set; }

        [Option("ConcurrentConnect", Required = false, Default = 10, HelpText = "Specify the concurrent connection number per second")]
        public int ConcurrentConnect { get; set; }

        [Option("SendSize", Required = false, Default = 2048, HelpText = "Specify the message size")]
        public int SendSize { get; set; }

        [Option("EnableTrace", Required = false, Default = 0, HelpText = "Specify 1 to enable client tracing, default is 0 (disabled)")]
        public int EnableTrace { get; set; }

        [Option("Hub", Required = false, Default = "chathub", HelpText = "Specify the hub name your app server defined. Default is 'chathub'")]
        public string HubName { get; set; }

        [Option("ServerMethod", Required = false, Default = "Echo", HelpText = "Specify the server method name your app server defined. Default is 'Echo'")]
        public string ServerMethod { get; set; }

        [Option("ClientMethod", Required = false, Default = "send", HelpText = "Specify the client method name your app server defined. Default is 'send'")]
        public string ClientMethod { get; set; }
    }
}
