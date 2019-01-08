using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace KafkaEventProducer
{
    public class Producer
    {
        private readonly Producer<byte[], byte[]> producer;
        private readonly Generator generator;
        private readonly string topic;
        

        public Producer(IConfiguration config, Generator generator)
        {
            var producerConfig =
                config.GetSection("ProducerConfig").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            this.generator = generator;
            topic = config["TopicName"];
            producer = new Producer<byte[], byte[]>(producerConfig);
        }

        public async Task<DeliveryReport<byte[], byte[]>> PublishNext()
        {
            var message = generator.NewMessage();
            return await producer.ProduceAsync(topic, message);
        }
    }
}