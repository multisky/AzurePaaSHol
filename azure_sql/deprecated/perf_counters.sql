SELECT object_name, counter_name, instance_name, cntr_value
FROM sys.dm_os_performance_counters
WHERE object_name like '%:Deprecated Features%';
GO 