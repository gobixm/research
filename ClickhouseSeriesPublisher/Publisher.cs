using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClickHouse.Ado;

namespace ClickhouseSeriesPublisher
{
    public class Publisher
    {
        private readonly int numDevices = 1000000;
        private readonly int numMetrics = 200;
        private readonly int numSamples = 10000000;
        private readonly int numSamplesInButch = 200000;
        private readonly int numServers = 4;
        private readonly Random random = new Random();

        private ClickHouseConnection GetConnection(string host, int port)
        {
            var connectionString =
                $"Compress=True;CheckCompressedHash=False;Compressor=lz4;Host={host};Port={port};Database=default;User=default;Password=;ConnectionTimeout=10000;SocketTimeout=10000; DataTransferTimeout=10000; MaxExecutionTime=10000;";
            var settings = new ClickHouseConnectionSettings(connectionString);
            var cnn = new ClickHouseConnection(settings);
            cnn.Open();
            return cnn;
        }

        public void Run(int concurrency)
        {
            if (concurrency > numServers) concurrency = numServers;

            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long total = 0;
            var sw = Stopwatch.StartNew();

            Parallel.For((long) 0, concurrency, server =>
            {
                while (total <= numSamples)
                    try
                    {
                        using (var cnn = GetConnection("localhost", 9011 + (short) server))
                        {
                            //INSERT INTO test_series.series_dist (entity, metric, v, ts) VALUES ('test', 'test', 1.0, 1546941002256);
                            var cmd = cnn.CreateCommand(
                                "INSERT INTO test_series.series_dist (entity, metric, v, ts) values @bulk;");

                            var batch = Enumerable.Range(0, numSamplesInButch).Select(x => new object[]
                            {
                                $"device-{(long) (random.NextDouble() * numDevices)}",
                                $"metric-{(long) (random.NextDouble() * numMetrics)}",
                                random.NextDouble(),
                                (ulong) (time + x)
                            }).ToArray();

                            cmd.Parameters.Add(new ClickHouseParameter
                            {
                                DbType = DbType.Object,
                                ParameterName = "bulk",
                                Value = batch
                            });
                            cmd.ExecuteNonQuery();
                            Interlocked.Add(ref total, numSamplesInButch);
                            Console.WriteLine($"processed:{total} in {sw.Elapsed.TotalSeconds} sec");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
            });
        }
    }
}