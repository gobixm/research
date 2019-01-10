using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Implementations;
using MQTTnet.Protocol;

namespace MqttPublisher
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var options1 = new MqttClientOptionsBuilder()
                .WithClientId("test_pub_1")
                .WithTcpServer("172.19.0.2")
                .WithTls()
                .WithCleanSession(false)
                .Build();

            var options2 = new MqttClientOptionsBuilder()
                .WithClientId("test_sub_2")
                .WithTcpServer("172.19.0.2")
                .WithTls()
                .WithCleanSession(false)
                .Build();

            var options3 = new MqttClientOptionsBuilder()
                .WithClientId("test_sub_3")
                .WithTcpServer("172.19.0.2")
                .WithTls()
                .WithCleanSession(false)
                .Build();

            MqttTcpChannel.CustomCertificateValidationCallback =
                (x509Certificate, x509Chain, sslPolicyErrors, mqttClientTcpOptions) => { return true; };

            var mqttClient1 = new MqttFactory().CreateMqttClient();
            var mqttClient2 = new MqttFactory().CreateMqttClient();
            var mqttClient3 = new MqttFactory().CreateMqttClient();

            var counter = 0;
            
            mqttClient1.Connected += (sender, eventArgs) => Console.WriteLine("connected");
            mqttClient1.Disconnected += (sender, eventArgs) => Console.WriteLine("disconnected");
            mqttClient1.ApplicationMessageReceived += (sender, eventArgs) =>
            {
                Console.WriteLine($"ApplicationMessageReceived {Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload)}");
            };
                

            mqttClient2.Connected += (sender, eventArgs) => Console.WriteLine("connected2");
            mqttClient2.Disconnected += (sender, eventArgs) => Console.WriteLine("disconnected2");
            mqttClient2.ApplicationMessageReceived += (sender, eventArgs) =>
            {
                Interlocked.Increment(ref counter);
                Console.WriteLine(
                    $"ApplicationMessageReceived2 {Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload)}");
            };
                

            mqttClient3.Connected += (sender, eventArgs) => Console.WriteLine("connected3");
            mqttClient3.Disconnected += (sender, eventArgs) => Console.WriteLine("disconnected3");
            mqttClient3.ApplicationMessageReceived += (sender, eventArgs) =>
            {
                Console.WriteLine(
                    $"ApplicationMessageReceived3 {Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload)}");
                Interlocked.Increment(ref counter);
            };

            mqttClient1.ConnectAsync(options1).Wait();
            mqttClient2.ConnectAsync(options2).Wait();
            mqttClient3.ConnectAsync(options3).Wait();

            mqttClient2.SubscribeAsync("$share/group/test-topic-b", MqttQualityOfServiceLevel.ExactlyOnce).Wait();
            mqttClient3.SubscribeAsync("$share/group/test-topic-b", MqttQualityOfServiceLevel.ExactlyOnce).Wait();
            

            Enumerable.Range(0, 100).ToList().ForEach(x => mqttClient1.PublishAsync("test-topic-b", $"load-{x}", MqttQualityOfServiceLevel.ExactlyOnce, false).Wait());
            
//            var tasks = Enumerable.Range(0, 100).Select(x => mqttClient1.PublishAsync("test-topic-a", $"load-{x}", MqttQualityOfServiceLevel.ExactlyOnce, false));
//
//            Task.WhenAll(tasks).Wait();


// StartAsync returns immediately, as it starts a new thread using Task.Run, 
// and so the calling thread needs to wait.
            Console.ReadLine();
            Console.WriteLine($"total: {counter}");
        }
    }
}