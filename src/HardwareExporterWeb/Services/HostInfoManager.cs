// // 张锐志 2023-09-15
using System.Data.SQLite;
using Microsoft.Data.Sqlite;
using NPoco;
using System.IO;
using HardwareExporterWeb.Models;
namespace HardwareExporterWeb.Services;

public class HostInfoManager
{
    private readonly ILogger<HostInfoManager> _logger;
    public HostInfoManager(ILogger<HostInfoManager> logger)
    {
        _logger = logger;
        InitTable();
    }
    
    private void InitTable()
    {
        _logger.LogInformation("cwd: {}",Directory.GetCurrentDirectory());
        try
        {
            const string windowsHostTableDDL = """

                                               CREATE TABLE IF NOT EXISTS HostInfo(
                                                   ID int primary key,
                                                   HostName TEXT not null default '',
                                                   HostIP TEXT not null default '' UNIQUE,
                                                   ExporterPort INTEGER not null default -1,
                                                   CreateTimestamp INTEGER not null default 0,
                                                   UpdateTimestamp INTEGER not null default 0
                                               );
                                               """;
            using var database = GetDatabase();
            database.Execute(windowsHostTableDDL);
        }
        catch (Exception e)
        {
            _logger.LogError("{e}",e);
        }
    }

    private IDatabase GetDatabase()
    {
        return new Database("Data Source=data.db", DatabaseType.SQLite, SqliteFactory.Instance);
    }
    
    public IEnumerable<HostInfoEntity> GetHostInfoEntities()
    {
        using var database = GetDatabase();
        return database.Fetch<HostInfoEntity>();
    }
    
    public IEnumerable<HostInfo> GetHostInfos()
    {
        using var database = GetDatabase();
        // InitTable();
        var entities = database.Fetch<HostInfoEntity>();
        return entities.Select(e => new HostInfo
        {
            HostIP = e.HostIP,
            HostName = e.HostName,
            ExporterPort = e.ExporterPort,
            CreateTime = DateTime.UnixEpoch.AddSeconds((double)e.CreateTimestamp!).ToLocalTime(),
            UpdateTime = DateTime.UnixEpoch.AddSeconds((double)e.UpdateTimestamp!).ToLocalTime()
        });
    }

    public HostInfoEntity GetHostInfoEntity(string hostIP)
    {
        using var database = GetDatabase();
        return database.Query<HostInfoEntity>().Where(h => h.HostIP == hostIP).First();
    }

    public HostInfo GetHostInfo(string hostIP)
    {
        var entity = GetHostInfoEntity(hostIP);
        return new HostInfo
        {
            HostName = entity.HostName,
            HostIP = entity.HostIP,
            ExporterPort = entity.ExporterPort,
            CreateTime = DateTime.UnixEpoch.AddSeconds((double)entity.CreateTimestamp!).ToLocalTime(),
            UpdateTime = DateTime.UnixEpoch.AddSeconds((double)entity.UpdateTimestamp!).ToLocalTime(),
        };
    }
    
    public void InsertHostInfo(HostInfoEntity hostInfoEntity)
    {
        using var database = GetDatabase();
        hostInfoEntity.CreateTimestamp ??= new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
        hostInfoEntity.UpdateTimestamp ??= new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
        database.Insert(hostInfoEntity);
    }

    public void InsertHostInfo(HostInfo hostInfo)
    {
        using var database = GetDatabase();
        var hostInfoEntity = new HostInfoEntity
        {
            ExporterPort = hostInfo.ExporterPort,
            HostName = hostInfo.HostName,
            HostIP = hostInfo.HostIP,
            CreateTimestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds(),
            UpdateTimestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds() 
        };
        database.Insert(hostInfoEntity);
    }
    
    public void DeleteHostInfo(string hostIP)
    {
        using var database = GetDatabase();
        database.DeleteWhere<HostInfo>($"HostIP = {hostIP}");
    }

    public void UpdateHostInfo(string hostIP, string? hostName, int? exporterPort)
    {
        using var database = GetDatabase();
        if (hostName is null && exporterPort is null) return;
        var setClause = $" UpdateTimestamp = {new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}";
        if (hostName != null)
        {
            setClause += $" , HostName = {hostName}";
        }
        if (exporterPort != null)
        {
            setClause += $" , ExporterPort = {exporterPort}";
        }
        var sql = @$"UPDATE HostInfo SET {setClause} WHERE HostIP = {hostIP}";
        _logger.LogInformation("update sql: {sql}",sql);
        database.Execute(sql);
    }

}
