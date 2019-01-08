DROP TABLE IF EXISTS test_series.inventory  ON CLUSTER test_cluster

CREATE TABLE IF NOT EXISTS test_series.inventory ON CLUSTER test_cluster
(
    id String,
    org_id String,
    dt DateTime default now()
) ENGINE = ReplacingMergeTree(dt)
PARTITION BY modulo(cityHash64(org_id), 10)
ORDER BY id

CREATE TABLE IF NOT EXISTS test_series.inventory_dist
    ON CLUSTER test_cluster AS test_series.inventory
    ENGINE = Distributed(test_cluster, test_series, inventory, cityHash64(org_id))