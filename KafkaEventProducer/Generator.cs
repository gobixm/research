using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KafkaEventProducer
{
    public class Generator
    {
        private readonly long buckets;
        private readonly long messageSize;
        private readonly Random seed = new Random();

        public Generator(IConfiguration config)
        {
            messageSize = long.Parse(config["MessageSize"]);
            buckets = long.Parse(config["Buckets"]);
        }

        public Message<byte[], byte[]> NewMessage()
        {
            var key = (long) (seed.NextDouble() * buckets);

            var metrics = new Dictionary<string, double>
            {
                {"cpu", seed.NextDouble()},
//                {"mem", seed.NextDouble() * 1000000},
//                {"processes", (int) (seed.NextDouble() * 100)}
            };

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(metrics));

            return new Message<byte[], byte[]>
            {
                Key = Encoding.UTF8.GetBytes($"pc-{key.ToString()}"),
                Timestamp = new Timestamp(DateTimeOffset.Now),
                Headers = new Headers
                {
                    {"type", new[] {(byte) 0}}
                },
//                Value = new byte[messageSize]
                Value = body
            };
        }
    }
}