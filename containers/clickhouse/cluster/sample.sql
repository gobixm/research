select metric, entity, tt as dt, v from (SELECT metric, entity, max(dt) as tt
    FROM test_series.series_dist t2
    WHERE t2.dt > '2019-01-08 10:00:40' and t2.metric='metric-1'
    GROUP BY metric, entity)
any left join (select metric, entity, dt, v from test_series.series_dist WHERE metric='metric-1')
using metric, entity, dt
order by metric, entity, dt
limit 1000

select metric from test_series.series_dist
WHERE metric='metric-22'
    and dt > '2019-01-08 11:20:40'
    and entity='device-11'
order by dt desc
limit 1000

