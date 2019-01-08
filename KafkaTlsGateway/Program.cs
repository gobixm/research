using System;
using System.IO;
using System.Linq;
using System.Net;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

namespace KafkaTlsGateway
{
    public class Program
    {
        public static void Main(string[] args)
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

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureKestrel(options =>
                {
                    options.Listen(IPAddress.Any, 8080, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                            listenOptions.UseHttps("cert.pfx", "123123123");
                        });
                })
                .UseStartup<Startup>();
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