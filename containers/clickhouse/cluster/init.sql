-- http://ui.tabix.io
create database test_series

DROP TABLE IF EXISTS test_series.series  ON CLUSTER test_cluster

CREATE TABLE IF NOT EXISTS test_series.series ON CLUSTER test_cluster
(
    entity String,
    metric String,
    v Float64,
    ts UInt64,
    dt DateTime DEFAULT toDateTime(round(ts/1000)) -- auto generate date time from ts column
) ENGINE = ReplicatedMergeTree('/clickhouse/tables/{shard}/series', '{replica}')
PARTITION BY (modulo(cityHash64(entity), 10), toMonday(dt))
ORDER BY (metric, dt, entity)

DROP TABLE IF EXISTS test_series.series_dist

CREATE TABLE IF NOT EXISTS test_series.series_dist
    ON CLUSTER test_cluster AS test_series.series
    ENGINE = Distributed(test_cluster, test_series, series, cityHash64(metric));

INSERT INTO test_series.series_dist (entity, metric, v, ts) VALUES ('test', 'test', 1.0, 1546941002256);