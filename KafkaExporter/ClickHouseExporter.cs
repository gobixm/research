using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ClickHouse.Ado;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KafkaExporter
{
    public class ClickHouseExporter
    {
        private Consumer<byte[], byte[]> consumer;
        private string topic;
        private long commitPeriod;
        public static long counter = 0;

        public ClickHouseExporter(IConfiguration config)
        {
            topic = config["TopicName"];
            commitPeriod = long.Parse(config["CommitPeriod"]);
            var consumerConfig =
                config.GetSection("ConsumerConfig").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            consumer = new Consumer<byte[], byte[]>(consumerConfig);

            //InitTable();
            
        }

        private void InitTable()
        {
            try
            {
                using (var con = GetConnection())
                {
                    string query = @"
DROP TABLE IF EXISTS test_series
";
                    var command = con.CreateCommand(query);
                    command.ExecuteNonQuery();

                    query = @"
CREATE TABLE test_series(
    entity String,
    ts UInt64, -- timestamp, milliseconds from January 1 1970
    m Array(String), -- names of the metrics
    v Array(Float32), -- values of the metrics
    d Date MATERIALIZED toDate(round(ts/1000)), -- auto generate date from ts column
    dt DateTime MATERIALIZED toDateTime(round(ts/1000)) -- auto generate date time from ts column
) ENGINE = MergeTree(d, entity, 8192)
";
                    command = con.CreateCommand(query);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            // echo "SELECT entity, dt, ts, v[indexOf(m, 'cpu')], v[indexOf(m, 'mem')] from test_series where entity like 'pc-999%'" | curl 'http://localhost:8123' --data-binary @-

        }

        private ClickHouseConnection GetConnection(string cstr= "Compress=True;CheckCompressedHash=False;Compressor=lz4;Host=localhost;Port=9001;Database=default;User=default;Password=;ConnectionTimeout=10000;SocketTimeout=10000")
        {
            var settings = new ClickHouseConnectionSettings(cstr);
            var cnn = new ClickHouseConnection(settings);
            cnn.Open();
            return cnn;
        }


        private struct Metrics
        {
            public string Key;
            public Dictionary<string, float> Values;
        }
        public void Run(CancellationToken token)
        {
            var metrics = new List<Metrics>();
            var batchCounter = 0;
            
            consumer.OnPartitionsAssigned += (_, partitions)
                    => Console.WriteLine($"Assigned partitions: [{string.Join(", ", partitions)}], member id: {consumer.MemberId}");

                // Raised when the consumer's current assignment set has been revoked.
                consumer.OnPartitionsRevoked += (_, partitions)
                    => Console.WriteLine($"Revoked partitions: [{string.Join(", ", partitions)}]");

                consumer.OnPartitionEOF += (_, tpo)
                    => Console.WriteLine($"Reached end of topic {tpo.Topic} partition {tpo.Partition}, next message will be at offset {tpo.Offset}");

                consumer.OnError += (_, e)
                    => Console.WriteLine($"Error: {e.Reason}");

                consumer.OnStatistics += (_, json)
                    => Console.WriteLine($"Statistics: {json}");

                consumer.Subscribe(topic);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(token);
//                        Console.WriteLine($"Topic: {consumeResult.Topic} Partition: {consumeResult.Partition} Offset: {consumeResult.Offset} {consumeResult.Value}");

                        var metric = new Metrics
                        {
                            Key = Encoding.UTF8.GetString(consumeResult.Key),
                            Values = JsonConvert.DeserializeObject<Dictionary<string, float>>(
                                Encoding.UTF8.GetString(consumeResult.Value))
                        };
                        
                        metrics.Add(metric);
                        batchCounter++;

                        if (batchCounter % commitPeriod == 0)
                        {   
                            var committedOffsets = consumer.Commit(consumeResult);
                            
                            try
                            {
                                var sw = Stopwatch.StartNew();
                                using (var cnn = GetConnection())
                                {
                                    var cmd = cnn.CreateCommand("INSERT INTO test_series (entity, ts, m, v) values @bulk;");
                                    
                                    var batch = metrics.Select(x =>
                                    {
                                        return new object[]
                                        {
                                            x.Key, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                            x.Values.Select(y => y.Key).ToArray(),
                                            x.Values.Select(y => (object) y.Value).ToArray()
                                        };
                                    }).ToArray();
                                    
                                    cmd.Parameters.Add(new ClickHouseParameter
                                    {
                                        DbType = DbType.Object,
                                        ParameterName = "bulk",
                                        Value = batch
                                    });
                                    cmd.ExecuteNonQuery();
                                }
                                Console.WriteLine($"{batchCounter} messages inserted to ClickHouse in {sw.Elapsed.TotalSeconds} secs");
                                batchCounter = 0;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }
                            metrics.Clear();
                            
                            Console.WriteLine($"Committed offset: {committedOffsets}");
                        }

                        Interlocked.Increment(ref counter);
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Consume error: {e.Error}");
                    }
                }

                consumer.Close();
        }
    }
}