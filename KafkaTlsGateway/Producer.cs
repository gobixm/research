using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace KafkaTlsGateway
{
    public class Producer
    {
        private readonly Producer<byte[], byte[]> producer;
        private readonly string topic;


        public Producer(IConfiguration config)
        {
            var producerConfig =
                config.GetSection("ProducerConfig").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            topic = config["TopicName"];
            
            producer = new Producer<byte[], byte[]>(producerConfig);
        }

        public async Task<DeliveryReport<byte[], byte[]>> PublishNext(byte[] key, byte[] body)
        {
            return await producer.ProduceAsync(topic, new Message<byte[], byte[]>
            {
                Key = key,
                Value = body
            });
        }
    }
}