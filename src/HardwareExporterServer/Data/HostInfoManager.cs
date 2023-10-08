// // 张锐志 2023-09-15
using System.Data.SQLite;
using Microsoft.Data.Sqlite;
using NPoco;
namespace HardwareExporterServer.Data;

public class HostInfoManager
{
    private readonly ILogger<HostInfoManager> _logger;
    private readonly IDatabase _database;
    public HostInfoManager(ILogger<HostInfoManager> logger)
    {
        _logger = logger;
        _database = new Database("Data Source=data.db", DatabaseType.SQLite, SqliteFactory.Instance);
        // InitTable();
    }

    private void InitTable()
    {
        try
        {
            const string windowsHostTableDDL = """

                                               CREATE TABLE IF NOT EXISTS host_info(
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

    public IEnumerable<HostInfo> GetHostInfos()
    {
        InitTable();
        return _database.Fetch<HostInfo>();
    }
    
    public void InsertHostInfo(HostInfo hostInfo)
    {
        _database.Insert(hostInfo);
    }

    public void DeleteHostInfo(int id)
    {
       var d= _database.SingleById<HostInfo>(id);
       _database.Delete<HostInfo>(d);
    }

}
