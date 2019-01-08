using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;

namespace KafkaEventProducer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("--clean to clean or --create to create");

            var config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"))
                .Build();

            if (args.Contains("--clean"))
            {
                DeleteTopic(config);
                return;
            }

            if (args.Contains("--create"))
            {
                CreateTopic(config);
                return;
            }

            var numMessages = int.Parse(config["NumMessages"]);
            var generator = new Generator(config);
            var producer = new Producer(config, generator);

            var sw = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, numMessages).Select(x => producer.PublishNext());
            Task.WhenAll(tasks).Wait();
            Console.WriteLine("{0} messages {1} bytes each in {2} secs.", numMessages, config["MessageSize"],
                sw.Elapsed.TotalSeconds);
            
            Console.WriteLine("random last message's offset is {0}", tasks.Last().Result.Offset.Value);
        }

        private static void DeleteTopic(IConfigurationRoot config)
        {
            var adminClientConfig = new AdminClientConfig
            {
                BootstrapServers = config["BootstrapServers"]
            };

            var adminClient = new AdminClient(adminClientConfig);

            var topic = config["TopicName"];
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            if (metadata.Topics.Any(x => x.Topic == topic)) adminClient.DeleteTopicsAsync(new[] {topic}).Wait();
        }

        private static void CreateTopic(IConfigurationRoot config)
        {
            var adminClientConfig = new AdminClientConfig
            {
                BootstrapServers = config["BootstrapServers"]
            };

            var adminClient = new AdminClient(adminClientConfig);

            var topic = config["TopicName"];
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            if (metadata.Topics.Any(x => x.Topic == topic)) return;

            var topicConfig = config.GetSection("TopicConfig").GetChildren().AsEnumerable()
                .ToDictionary(x => x.Key, x => x.Value);

            adminClient.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = int.Parse(config["NumPartitions"]),
                    ReplicationFactor = short.Parse(config["ReplicationFactor"]),
                    Configs = topicConfig
                }
            }).Wait();
        }
    }
}