using CommandLine;
using System;
using System.Threading.Tasks;

namespace aspnetclient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var argsOption = ParseArgs(args);
            if (argsOption == null)
            {
                return;
            }
            var counter = new Counter();
            var cc = new ClientConnection(argsOption, counter);
            await cc.BatchStartConnect(argsOption.ConcurrentConnect > argsOption.Clients ?
                                       argsOption.Clients : argsOption.ConcurrentConnect);
            cc.StartSend();
            counter.StartPrint();
            Console.Read();
            cc.Dispose();
        }

        private static ArgsOption ParseArgs(string[] args)
        {
            bool e = false;
            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error =>
                {
                    Console.WriteLine($"Error in parsing arguments: {error}");
                    e = true;
                });
            if (e)
                return null;
            return argsOption;
        }
    }
}
