// // 张锐志 2023-09-15
using System.Data.SQLite;
using Microsoft.Data.Sqlite;
using NPoco;
using HardwareExporterServer.Model;
namespace HardwareExporterServer.Services;

public class HostInfoManager
{
    private readonly ILogger<HostInfoManager> _logger;
    private readonly IDatabase _database;
    public HostInfoManager(ILogger<HostInfoManager> logger)
    {
        _logger = logger;
        _database = new Database("Data Source=data.db", DatabaseType.SQLite, SqliteFactory.Instance);
        InitTable();
    }

    private void InitTable()
    {
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
            _database.Execute(windowsHostTableDDL);
        }
        catch (Exception e)
        {
            _logger.LogError("{e}",e);
        }
    }

    public IEnumerable<HostInfoEntity> GetHostInfoEntities()
    {
        // InitTable();
        return _database.Fetch<HostInfoEntity>();
    }
    
    public IEnumerable<HostInfo> GetHostInfos()
    {
        // InitTable();
        var entities = _database.Fetch<HostInfoEntity>();
        return entities.Select(e => new HostInfo
        {
            HostIP = e.HostIP,
            HostName = e.HostName,
            ExporterPort = e.ExporterPort,
            CreateTime = DateTime.UnixEpoch.AddSeconds((double)e.CreateTimestamp!),
            UpdateTime = DateTime.UnixEpoch.AddSeconds((double)e.UpdateTimestamp!)
        });
    }
    
    public void InsertHostInfo(HostInfoEntity hostInfoEntity)
    {
        hostInfoEntity.CreateTimestamp ??= new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
        hostInfoEntity.UpdateTimestamp ??= new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
        _database.Insert(hostInfoEntity);
    }

    public void DeleteHostInfo(int id)
    {
       var d= _database.SingleById<HostInfoEntity>(id);
       _database.Delete<HostInfoEntity>(d);
    }

}
