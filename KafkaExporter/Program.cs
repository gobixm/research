using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace KafkaExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"))
                .Build();

            var cancellation = new CancellationTokenSource();
            Timer timer = new Timer(state =>
            {
                Console.WriteLine($"{ClickHouseExporter.counter} messages in sec");
                ClickHouseExporter.counter = 0;
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            Task.Run(() =>
            {
                var exporter = new ClickHouseExporter(config);
                exporter.Run(cancellation.Token);
            }, cancellation.Token);
            
            Console.WriteLine("press something to quit");
            Console.Read();
            cancellation.Cancel();
        }
    }
}