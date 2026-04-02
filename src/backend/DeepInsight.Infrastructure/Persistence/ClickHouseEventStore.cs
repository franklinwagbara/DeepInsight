using System.Data;
using System.Data.Common;
using System.Text.Json;
using ClickHouse.Client.ADO;
using ClickHouse.Client.ADO.Parameters;
using ClickHouse.Client.Copy;
using DeepInsight.Core.Interfaces;
using DeepInsight.Core.Models;

namespace DeepInsight.Infrastructure.Persistence;

public class ClickHouseEventStore : IEventStore
{
    private readonly string _connectionString;

    public ClickHouseEventStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task EnsureTablesAsync()
    {
        using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS events (
                project_id UUID,
                session_id String,
                timestamp UInt64,
                type String,
                page_url String,
                data String
            ) ENGINE = MergeTree()
            ORDER BY (project_id, session_id, timestamp)";
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS heatmap_clicks (
                project_id UUID,
                page_url String,
                heatmap_type String,
                x_pct Float32,
                y_pct Float32,
                count UInt32,
                date Date
            ) ENGINE = SummingMergeTree(count)
            ORDER BY (project_id, page_url, heatmap_type, date, x_pct, y_pct)";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task InsertEventsAsync(IEnumerable<TrackingEvent> events, CancellationToken ct = default)
    {
        var eventList = events.ToList();
        if (eventList.Count == 0) return;

        using var conn = new ClickHouseConnection(_connectionString);
        using var bulk = new ClickHouseBulkCopy(conn)
        {
            DestinationTableName = "events",
            BatchSize = eventList.Count
        };

        var rows = eventList.Select(e => new object[]
        {
            e.ProjectId,
            e.SessionId,
            (ulong)e.Timestamp,
            e.Type,
            e.PageUrl,
            e.Data.GetRawText()
        });

        await bulk.InitAsync();
        await bulk.WriteToServerAsync(rows, ct);
    }

    public async Task<IReadOnlyList<TrackingEvent>> GetSessionEventsAsync(Guid projectId, string sessionId, CancellationToken ct = default)
    {
        using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(ct);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT project_id, session_id, timestamp, type, page_url, data
            FROM events
            WHERE project_id = {projectId:UUID} AND session_id = {sessionId:String}
            ORDER BY timestamp ASC";
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "projectId", Value = projectId, DbType = DbType.Guid });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "sessionId", Value = sessionId, DbType = DbType.String });

        var results = new List<TrackingEvent>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new TrackingEvent
            {
                ProjectId = reader.GetGuid(0),
                SessionId = reader.GetString(1),
                Timestamp = Convert.ToInt64(reader.GetValue(2)),
                Type = reader.GetString(3),
                PageUrl = reader.GetString(4),
                Data = JsonDocument.Parse(reader.GetString(5)).RootElement
            });
        }
        return results;
    }

    public async Task<IReadOnlyList<HeatmapData>> GetClickHeatmapAsync(Guid projectId, string pageUrl, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await GetHeatmapAsync(projectId, pageUrl, "click", from, to, ct);
    }

    public async Task<IReadOnlyList<HeatmapData>> GetScrollHeatmapAsync(Guid projectId, string pageUrl, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await GetHeatmapAsync(projectId, pageUrl, "scroll", from, to, ct);
    }

    private async Task<IReadOnlyList<HeatmapData>> GetHeatmapAsync(Guid projectId, string pageUrl, string heatmapType, DateTime from, DateTime to, CancellationToken ct)
    {
        using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(ct);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT project_id, page_url, heatmap_type, x_pct, y_pct, sum(count) as total_count, date
            FROM heatmap_clicks
            WHERE project_id = {projectId:UUID}
              AND page_url = {pageUrl:String}
              AND heatmap_type = {heatmapType:String}
              AND date >= {from:Date}
              AND date <= {to:Date}
            GROUP BY project_id, page_url, heatmap_type, x_pct, y_pct, date
            ORDER BY total_count DESC
            LIMIT 10000";
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "projectId", Value = projectId, DbType = DbType.Guid });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "pageUrl", Value = pageUrl, DbType = DbType.String });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "heatmapType", Value = heatmapType, DbType = DbType.String });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "from", Value = from, DbType = DbType.Date });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "to", Value = to, DbType = DbType.Date });

        var results = new List<HeatmapData>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new HeatmapData
            {
                ProjectId = reader.GetGuid(0),
                PageUrl = reader.GetString(1),
                HeatmapType = reader.GetString(2),
                XPct = reader.GetFloat(3),
                YPct = reader.GetFloat(4),
                Count = Convert.ToInt32(reader.GetValue(5)),
                Date = reader.GetDateTime(6)
            });
        }
        return results;
    }

    public async Task InsertHeatmapDataAsync(IEnumerable<HeatmapData> data, CancellationToken ct = default)
    {
        var dataList = data.ToList();
        if (dataList.Count == 0) return;

        using var conn = new ClickHouseConnection(_connectionString);
        using var bulk = new ClickHouseBulkCopy(conn)
        {
            DestinationTableName = "heatmap_clicks",
            BatchSize = dataList.Count
        };

        var rows = dataList.Select(d => new object[]
        {
            d.ProjectId,
            d.PageUrl,
            d.HeatmapType,
            d.XPct,
            d.YPct,
            (uint)d.Count,
            d.Date
        });

        await bulk.InitAsync();
        await bulk.WriteToServerAsync(rows, ct);
    }

    public async Task<long> GetTotalEventsAsync(Guid projectId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(ct);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT count()
            FROM events
            WHERE project_id = {projectId:UUID}
              AND timestamp >= {from:UInt64}
              AND timestamp <= {to:UInt64}";
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "projectId", Value = projectId, DbType = DbType.Guid });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "from", Value = (ulong)new DateTimeOffset(from).ToUnixTimeMilliseconds(), DbType = DbType.UInt64 });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "to", Value = (ulong)new DateTimeOffset(to).ToUnixTimeMilliseconds(), DbType = DbType.UInt64 });

        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }
}
