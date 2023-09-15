// // 张锐志 2023-09-15
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
        _database = new Database("./data.db", DatabaseType.SQLite, SqliteFactory.Instance);
    }

    public IEnumerable<HostInfo> GetHostInfos()
    {
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
